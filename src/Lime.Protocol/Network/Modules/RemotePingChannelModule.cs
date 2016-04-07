using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Client;
using Lime.Protocol.Server;

namespace Lime.Protocol.Network.Modules
{
    /// <summary>
    /// Defines a module that pings the remote party after a period of inactivity.
    /// </summary>
    public sealed class RemotePingChannelModule : IChannelModule<Message>, IChannelModule<Notification>, IChannelModule<Command>, IDisposable
    {
        public const string PING_URI = "/ping";
        private static readonly TimeSpan DefaultFinishChannelTimeout = TimeSpan.FromSeconds(30);

        private readonly IChannel _channel;
        private readonly TimeSpan _remotePingInterval;
        private readonly TimeSpan _remoteIdleTimeout;
        private readonly TimeSpan _finishChannelTimeout;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly object _syncRoot = new object();
       
        private Task _pingRemoteTask;        

        private RemotePingChannelModule(IChannel channel, TimeSpan remotePingInterval, TimeSpan? remoteIdleTimeout = null, TimeSpan? finishChannelTimeout = null)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            _channel = channel;
            _remotePingInterval = remotePingInterval;
            _remoteIdleTimeout = remoteIdleTimeout ?? TimeSpan.Zero;
            _finishChannelTimeout = finishChannelTimeout ?? DefaultFinishChannelTimeout;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public DateTimeOffset LastReceivedEnvelope { get; private set; }


        public void OnStateChanged(SessionState state)
        {
            lock (_syncRoot)
            {
                if (state == SessionState.Established &&
                    _pingRemoteTask == null)
                {
                    _pingRemoteTask = Task.Run(PingRemoteAsync);
                }
                else if (state > SessionState.Established &&
                    !_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
        }

        public Task<Message> OnReceivingAsync(Message envelope, CancellationToken cancellationToken)
        {
            return ReceiveEnvelope(envelope);
        }

        public Task<Message> OnSendingAsync(Message envelope, CancellationToken cancellationToken)
        {
            return envelope.AsCompletedTask();
        }

        public Task<Notification> OnReceivingAsync(Notification envelope, CancellationToken cancellationToken)
        {
            return ReceiveEnvelope(envelope);
        }

        public Task<Notification> OnSendingAsync(Notification envelope, CancellationToken cancellationToken)
        {
            return envelope.AsCompletedTask();
        }

        public Task<Command> OnReceivingAsync(Command envelope, CancellationToken cancellationToken)
        {
            return ReceiveEnvelope(envelope);
        }

        public Task<Command> OnSendingAsync(Command envelope, CancellationToken cancellationToken)
        {
            return envelope.AsCompletedTask();
        }

        /// <summary>
        /// Creates a new instance of <see cref="RemotePingChannelModule"/> class and register it to the specified channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="remotePingInterval">The remote ping interval.</param>
        /// <param name="remoteIdleTimeout">The remote idle timeout.</param>
        public static RemotePingChannelModule CreateAndRegister(IChannel channel, TimeSpan remotePingInterval, TimeSpan? remoteIdleTimeout = null)
        {
            var remotePingChannelModule = new RemotePingChannelModule(channel, remotePingInterval, remoteIdleTimeout);
            channel.MessageModules.Add(remotePingChannelModule);
            channel.NotificationModules.Add(remotePingChannelModule);
            channel.CommandModules.Add(remotePingChannelModule);
            return remotePingChannelModule;
        }

        private Task<T> ReceiveEnvelope<T>(T envelope) where T : Envelope, new()
        {
            LastReceivedEnvelope = DateTimeOffset.UtcNow;
            return envelope.AsCompletedTask();
        }

        private async Task PingRemoteAsync()
        {
            LastReceivedEnvelope = DateTime.UtcNow;

            while (!_cancellationTokenSource.IsCancellationRequested && 
                _channel.State == SessionState.Established && 
                _channel.Transport.IsConnected)
            {
                try
                {
                    await Task.Delay(_remotePingInterval, _cancellationTokenSource.Token).ConfigureAwait(false);

                    if (_channel.State != SessionState.Established || !_channel.Transport.IsConnected) continue;

                    var idleTime = DateTimeOffset.UtcNow - LastReceivedEnvelope;
                    if (_remoteIdleTimeout > TimeSpan.Zero &&
                        idleTime >= _remoteIdleTimeout)
                    {
                        using (var cts = new CancellationTokenSource(_finishChannelTimeout))
                        {
                            if (_channel is IClientChannel)
                            {
                                await FinishAsync((IClientChannel)_channel, cts.Token).ConfigureAwait(false);

                            }
                            else if (_channel is IServerChannel)
                            {
                                await FinishAsync((IServerChannel)_channel, cts.Token).ConfigureAwait(false);
                            }
                        }
                    }
                    else if (idleTime >= _remotePingInterval)
                    {
                        // Send a ping command to the remote party
                        var pingCommandRequest = new Command(Guid.NewGuid())
                        {
                            Method = CommandMethod.Get,
                            Uri = new LimeUri(PING_URI)
                        };

                        using (var cts = new CancellationTokenSource(_remotePingInterval))
                        {
                            await
                                _channel.SendCommandAsync(pingCommandRequest, cts.Token)
                                    .ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private static async Task FinishAsync(IClientChannel clientChannel, CancellationToken cancellationToken)
        {
            var receivedFinishedSessionTask = clientChannel.ReceiveFinishedSessionAsync(cancellationToken);
            await clientChannel.SendFinishingSessionAsync(cancellationToken).ConfigureAwait(false);
            await receivedFinishedSessionTask.ConfigureAwait(false);
        }

        private static Task FinishAsync(IServerChannel serverChannel, CancellationToken cancellationToken)
        {
            return serverChannel.SendFinishedSessionAsync(cancellationToken);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
            _pingRemoteTask?.Dispose();
        }
    }
}
