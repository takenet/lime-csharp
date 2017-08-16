using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;
using Lime.Protocol.Security;

namespace Lime.Protocol.Server
{
    /// <summary>
    /// Simple generic server for receiving connections and processing envelopes.
    /// </summary>
    public class Server : IServer
    {               
        private readonly ITransportListener _transportListener;
        private readonly Func<ITransport, IServerChannel> _serverChannelFactory;
        private readonly SessionCompression[] _enabledCompressionOptions;
        private readonly SessionEncryption[] _enabledEncryptionOptions;
        private readonly AuthenticationScheme[] _schemeOptions;
        private readonly Func<Node, Authentication, Task<AuthenticationResult>> _authenticator;
        private readonly Func<IChannelListener> _channelListenerFactory;
        private readonly Func<Exception, Task> _exceptionHandler;
        private readonly int _maxActiveChannels;

        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentDictionary<Node, IServerChannel> _nodeChannelsDictionary;

        private CancellationTokenSource _listenerCts;
        private Task _listenerTask;
        private ITargetBlock<IServerChannel> _consumerBlock;
        
        public Server(
            ITransportListener transportListener,
            Func<ITransport, IServerChannel> serverChannelFactory,
            SessionCompression[] enabledCompressionOptions,
            SessionEncryption[] enabledEncryptionOptions,
            AuthenticationScheme[] schemeOptions,
            Func<Node, Authentication, Task<AuthenticationResult>> authenticator,
            Func<IChannelListener> channelListenerFactory,
            Func<Exception, Task> exceptionHandler = null,
            int maxActiveChannels = -1)
        {
            _transportListener = transportListener ?? throw new ArgumentNullException(nameof(transportListener));
            _serverChannelFactory = serverChannelFactory ?? throw new ArgumentNullException(nameof(serverChannelFactory));
            _enabledCompressionOptions = enabledCompressionOptions ?? throw new ArgumentNullException(nameof(enabledCompressionOptions));
            _enabledEncryptionOptions = enabledEncryptionOptions ?? throw new ArgumentNullException(nameof(enabledEncryptionOptions));
            _schemeOptions = schemeOptions ?? throw new ArgumentNullException(nameof(schemeOptions));
            _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            _channelListenerFactory = channelListenerFactory ?? throw new ArgumentNullException(nameof(channelListenerFactory));
            _exceptionHandler = exceptionHandler;
            _maxActiveChannels = maxActiveChannels;
            _semaphore = new SemaphoreSlim(1, 1);
            _nodeChannelsDictionary = new ConcurrentDictionary<Node, IServerChannel>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_listenerTask != null) throw new InvalidOperationException("The server is already started");

                _consumerBlock = new ActionBlock<IServerChannel>(
                    ConsumeAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        BoundedCapacity = _maxActiveChannels,
                        MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
                    });

                await _transportListener.StartAsync(cancellationToken).ConfigureAwait(false);

                _listenerCts = new CancellationTokenSource();
                _listenerTask = Task.Run((Func<Task>) (() => ListenAsync(_listenerCts.Token)));
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_listenerTask == null) throw new InvalidOperationException("The server is not started");
                _listenerCts.Cancel();
                _consumerBlock.Complete();
                await Task.WhenAll(_listenerTask, _consumerBlock.Completion).ConfigureAwait(false);
                _listenerCts.Dispose();
                _listenerCts = null;
                _listenerTask = null;
                _consumerBlock = null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public IServerChannel GetChannel(Node remoteNode)
        {
            _nodeChannelsDictionary.TryGetValue(remoteNode, out var channel);
            return channel;
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var transport = await _transportListener.AcceptTransportAsync(cancellationToken).ConfigureAwait(false);
                    var channel = _serverChannelFactory(transport);
                    if (!await DataflowBlock.SendAsync<IServerChannel>(_consumerBlock, channel, cancellationToken))
                    {
                        // The server pipeline is complete
                        break;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task ConsumeAsync(IServerChannel serverChannel)
        {
            try
            {
                await serverChannel.EstablishSessionAsync(
                        serverChannel.Transport.GetSupportedCompression().Intersect(_enabledCompressionOptions)
                            .ToArray(),
                        serverChannel.Transport.GetSupportedEncryption().Intersect(_enabledEncryptionOptions).ToArray(),
                        _schemeOptions,
                        _authenticator,
                        _listenerCts.Token)
                    .ConfigureAwait(false);

                if (serverChannel.State == SessionState.Established)
                {
                    if (!_nodeChannelsDictionary.TryAdd(serverChannel.RemoteNode, serverChannel))
                    {
                        await serverChannel.SendFailedSessionAsync(new Reason()
                        {
                            Code = ReasonCodes.SESSION_ERROR,
                            Description = "Could not register the channel node"
                        }, _listenerCts.Token);

                        return;
                    }

                    var channelListener = _channelListenerFactory();

                    try
                    {


                        // Consume the channel envelopes
                        channelListener.Start(serverChannel);

                        // Awaits for the finishing envelope
                        var finishingSessionTask = serverChannel.ReceiveFinishingSessionAsync(_listenerCts.Token);

                        // Stops the consumer when any of the tasks finishes
                        await
                            Task.WhenAny(
                                finishingSessionTask,
                                channelListener.MessageListenerTask,
                                channelListener.CommandListenerTask,
                                channelListener.NotificationListenerTask);

                        if (finishingSessionTask.IsCompleted)
                        {
                            await serverChannel.SendFinishedSessionAsync(_listenerCts.Token);
                        }
                    }
                    finally
                    {
                        channelListener.Stop();

                        if (serverChannel.RemoteNode != null)
                        {
                            _nodeChannelsDictionary.TryRemove(serverChannel.RemoteNode, out _);
                        }
                    }
                }

                // If something bizarre occurs
                if (serverChannel.State != SessionState.Finished &&
                    serverChannel.State != SessionState.Failed)
                {
                    await serverChannel.SendFailedSessionAsync(new Reason()
                    {
                        Code = ReasonCodes.SESSION_ERROR,
                        Description = "The session failed"
                    }, _listenerCts.Token);
                }
            }
            catch (OperationCanceledException) when (_listenerCts.IsCancellationRequested)
            {
                await serverChannel.SendFailedSessionAsync(new Reason()
                {
                    Code = ReasonCodes.SESSION_ERROR,
                    Description = "The server is being shut down"
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                if (_exceptionHandler != null)
                {
                    await _exceptionHandler(ex).ConfigureAwait(false);
                }
            }
            finally
            {
                serverChannel.DisposeIfDisposable();
            }
        }
    }
}