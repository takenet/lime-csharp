using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Client;
using Lime.Protocol.Server;

namespace Lime.Protocol.Network.Modules
{
    /// <summary>
    /// Defines a module that pings the remote party after a period of inactivity.
    /// </summary>
    public class RemotePingChannelModule : IChannelModule<Message>, IChannelModule<Notification>, IChannelModule<Command>, IDisposable
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
        private string _lastPingCommandRequestId;
        private bool _hasPendingPingRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemotePingChannelModule"/> class.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="remotePingInterval">The remote ping interval.</param>
        /// <param name="remoteIdleTimeout">The remote idle timeout.</param>
        /// <param name="finishChannelTimeout">The finish channel timeout.</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected RemotePingChannelModule(
            IChannel channel, 
            TimeSpan remotePingInterval, 
            TimeSpan? remoteIdleTimeout = null, 
            TimeSpan? finishChannelTimeout = null)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _remotePingInterval = remotePingInterval;
            if (remoteIdleTimeout != null &&
                remoteIdleTimeout.Value != TimeSpan.Zero &&
                remoteIdleTimeout.Value < remotePingInterval)
            {
                throw new ArgumentException("Remote idle timeout cannot be smaller than remote ping interval", nameof(remoteIdleTimeout));
            }
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

        public Task<Message> OnReceivingAsync(Message envelope, CancellationToken cancellationToken) => ReceiveEnvelope(envelope);

        public Task<Message> OnSendingAsync(Message envelope, CancellationToken cancellationToken) => envelope.AsCompletedTask();

        public Task<Notification> OnReceivingAsync(Notification envelope, CancellationToken cancellationToken) => ReceiveEnvelope(envelope);

        public Task<Notification> OnSendingAsync(Notification envelope, CancellationToken cancellationToken) => envelope.AsCompletedTask();

        public Task<Command> OnReceivingAsync(Command envelope, CancellationToken cancellationToken)
        {
            var receivedEnvelopeTask = ReceiveEnvelope(envelope);
            if (envelope.Status == CommandStatus.Success && 
                envelope.Method == CommandMethod.Get &&
                _lastPingCommandRequestId != null &&
                envelope.Id != null &&
                envelope.Id.Equals(_lastPingCommandRequestId))
            {
                _hasPendingPingRequest = false;
                // Suppress the receiving of a ping response command
                return Task.FromResult<Command>(null);
            }
            return receivedEnvelopeTask;
        }

        public Task<Command> OnSendingAsync(Command envelope, CancellationToken cancellationToken) => envelope.AsCompletedTask();

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
                    if (_hasPendingPingRequest && 
                        _remoteIdleTimeout > TimeSpan.Zero && 
                        idleTime >= _remoteIdleTimeout)
                    {
                        using (var cts = new CancellationTokenSource(_finishChannelTimeout))
                        {
                            switch (_channel)
                            {
                                case IClientChannel clientChannel:
                                    await FinishAsync(clientChannel, cts.Token).ConfigureAwait(false);
                                    break;
                                case IServerChannel serverChannel:
                                    await FinishAsync(serverChannel, cts.Token).ConfigureAwait(false);
                                    break;
                            }
                        }
                    }
                    else if (idleTime >= _remotePingInterval)
                    {
                        _lastPingCommandRequestId = EnvelopeId.NewId();
                        // Send a ping command to the remote party
                        var pingCommandRequest = new Command(_lastPingCommandRequestId)
                        {
                            Method = CommandMethod.Get,
                            Uri = new LimeUri(PING_URI)
                        };

                        _hasPendingPingRequest = true;

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

        protected virtual async Task FinishAsync(IClientChannel clientChannel, CancellationToken cancellationToken)
        {
            var receivedFinishedSessionTask = clientChannel.ReceiveFinishedSessionAsync(cancellationToken);
            await clientChannel.SendFinishingSessionAsync(cancellationToken).ConfigureAwait(false);
            await receivedFinishedSessionTask.ConfigureAwait(false);
        }

        protected virtual Task FinishAsync(IServerChannel serverChannel, CancellationToken cancellationToken)
        {
            return serverChannel.SendFinishedSessionAsync(cancellationToken);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }
    }
}