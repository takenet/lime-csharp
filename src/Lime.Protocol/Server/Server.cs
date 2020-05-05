using System;
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
        private readonly Func<Identity, Authentication, CancellationToken, Task<AuthenticationResult>> _authenticator;
        private readonly Func<IChannelInformation, IChannelListener> _channelListenerFactory;
        private readonly INodeRegistry _nodeRegistry;
        private readonly Func<Exception, Task<bool>> _exceptionHandler;
        private readonly int _maxActiveChannels;

        private readonly SemaphoreSlim _semaphore;
        

        private CancellationTokenSource _listenerCts;
        private Task _listenerTask;
        private ITargetBlock<IServerChannel> _consumerBlock;

        public Server(
            ITransportListener transportListener,
            Func<ITransport, IServerChannel> serverChannelFactory,
            SessionCompression[] enabledCompressionOptions,
            SessionEncryption[] enabledEncryptionOptions,
            AuthenticationScheme[] schemeOptions,
            Func<Identity, Authentication, CancellationToken, Task<AuthenticationResult>> authenticator,
            Func<IChannelInformation, IChannelListener> channelListenerFactory,
            INodeRegistry nodeRegistry = null,
            Func<Exception, Task<bool>> exceptionHandler = null,
            int maxActiveChannels = -1)
        {
            _transportListener = transportListener ?? throw new ArgumentNullException(nameof(transportListener));
            _serverChannelFactory = serverChannelFactory ?? throw new ArgumentNullException(nameof(serverChannelFactory));
            _enabledCompressionOptions = enabledCompressionOptions ?? throw new ArgumentNullException(nameof(enabledCompressionOptions));
            _enabledEncryptionOptions = enabledEncryptionOptions ?? throw new ArgumentNullException(nameof(enabledEncryptionOptions));
            _schemeOptions = schemeOptions ?? throw new ArgumentNullException(nameof(schemeOptions));
            _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            _channelListenerFactory = channelListenerFactory ?? throw new ArgumentNullException(nameof(channelListenerFactory));
            _nodeRegistry = nodeRegistry ?? new NodeRegistry();
            _exceptionHandler = exceptionHandler;
            _maxActiveChannels = maxActiveChannels;
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_listenerTask != null) throw new InvalidOperationException("The server is already started");

                // Initialize a block for holding the channel consumer tasks.
                _consumerBlock = new ActionBlock<IServerChannel>(
                    ConsumeAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        BoundedCapacity = _maxActiveChannels,
                        MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                        EnsureOrdered = false
                    });

                await _transportListener.StartAsync(cancellationToken).ConfigureAwait(false);

                // Initialize a background task for listening for new transport connections
                _listenerCts = new CancellationTokenSource();
                _listenerTask = Task.Run(() => AcceptTransportsAsync(_listenerCts.Token));
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
                await _transportListener.StopAsync(cancellationToken);
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

        private async Task AcceptTransportsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var transport = await _transportListener.AcceptTransportAsync(cancellationToken)
                        .ConfigureAwait(false);
                    await transport.OpenAsync(null, cancellationToken).ConfigureAwait(false);
                    var channel = _serverChannelFactory(transport);
                    if (!await _consumerBlock.SendAsync(channel, cancellationToken))
                    {
                        // The server pipeline is complete
                        break;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_exceptionHandler == null ||
                        !await _exceptionHandler(ex).ConfigureAwait(false))
                    {
                        throw;
                    }
                }
            }
        }

        private async Task ConsumeAsync(IServerChannel serverChannel)
        {
            try
            {
                // Establishes the session
                await serverChannel.EstablishSessionAsync(
                        serverChannel.Transport.GetSupportedCompression().Intersect(_enabledCompressionOptions)
                            .ToArray(),
                        serverChannel.Transport.GetSupportedEncryption().Intersect(_enabledEncryptionOptions).ToArray(),
                        _schemeOptions,
                        _authenticator,
                        _nodeRegistry.TryRegisterAsync,
                        _listenerCts.Token)
                    .ConfigureAwait(false);

                if (serverChannel.State == SessionState.Established)
                {
                    await ListenAsync(serverChannel);
                }

                // If something bizarre occurs
                if (serverChannel.IsActive())
                {
                    await serverChannel.SendFailedSessionAsync(
                        new Reason()
                        {
                            Code = ReasonCodes.SESSION_ERROR,
                            Description = "The session was terminated by the server"
                        }, 
                        _listenerCts.Token);
                }
            }
            catch (OperationCanceledException) when (_listenerCts.IsCancellationRequested)
            {
                if (serverChannel.IsActive())
                {
                    await serverChannel.SendFailedSessionAsync(
                        new Reason()
                        {
                            Code = ReasonCodes.SESSION_ERROR,
                            Description = "The server is being shut down"
                        },
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                if (_exceptionHandler != null)
                {
                    await _exceptionHandler(ex).ConfigureAwait(false);
                }
                
                if (serverChannel.IsActive())
                {
                    await serverChannel.SendFailedSessionAsync(
                        new Reason()
                        {
                            Code = ReasonCodes.SESSION_ERROR,
                            Description = "An unexpected server error occurred"
                        },
                        CancellationToken.None);
                }
            }
            finally
            {
                serverChannel.DisposeIfDisposable();
            }
        }

        private async Task ListenAsync(IServerChannel serverChannel)
        {
            // Initializes a new consumer
            var channelListener = _channelListenerFactory(serverChannel);

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

                using var cts  = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _nodeRegistry.UnregisterAsync(serverChannel.RemoteNode, cts.Token);
            }
        }
    }
}