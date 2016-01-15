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
    public sealed class RemotePingChannelModule : IChannelModule<Message>, IChannelModule<Notification>, IChannelModule<Command>, IDisposable
    {
        public const string PING_URI = "/ping";
        private static readonly TimeSpan OnRemoteIdleTimeout = TimeSpan.FromSeconds(30);

        private readonly IChannel _channel;
        private readonly TimeSpan _remotePingInterval;
        private readonly TimeSpan _remoteIdleTimeout;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly object _syncRoot = new object();
       
        private Task _pingRemoteTask;        

        private RemotePingChannelModule(IChannel channel, TimeSpan remotePingInterval, TimeSpan? remoteIdleTimeout = null)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            _channel = channel;
            _remotePingInterval = remotePingInterval;
            _remoteIdleTimeout = remoteIdleTimeout ?? TimeSpan.Zero;
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
                    _pingRemoteTask =
                        Task.Factory.StartNew(PingRemoteAsync)
                            .Unwrap();
                }
                else if (state > SessionState.Established &&
                    !_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
        }

        public Task<Message> OnReceiving(Message envelope, CancellationToken cancellationToken)
        {
            return ReceiveEnvelope(envelope);
        }

        public Task<Message> OnSending(Message envelope, CancellationToken cancellationToken)
        {
            return envelope.AsCompletedTask();
        }

        public Task<Notification> OnReceiving(Notification envelope, CancellationToken cancellationToken)
        {
            return ReceiveEnvelope(envelope);
        }

        public Task<Notification> OnSending(Notification envelope, CancellationToken cancellationToken)
        {
            return envelope.AsCompletedTask();
        }

        public Task<Command> OnReceiving(Command envelope, CancellationToken cancellationToken)
        {
            return ReceiveEnvelope(envelope);
        }

        public Task<Command> OnSending(Command envelope, CancellationToken cancellationToken)
        {
            return envelope.AsCompletedTask();
        }

        public static void Register(IChannel channel, TimeSpan remotePingInterval, TimeSpan? remoteIdleTimeout = null)
        {
            var remotePingChannelModule = new RemotePingChannelModule(channel, remotePingInterval, remoteIdleTimeout);
            channel.MessageModules.Add(remotePingChannelModule);
            channel.NotificationModules.Add(remotePingChannelModule);
            channel.CommandModules.Add(remotePingChannelModule);
        }

        private Task<T> ReceiveEnvelope<T>(T envelope) where T : Envelope, new()
        {
            LastReceivedEnvelope = DateTimeOffset.UtcNow;
            return envelope.AsCompletedTask();
        }

        private async Task PingRemoteAsync()
        {
            LastReceivedEnvelope = DateTime.UtcNow;

            // Awaits for the session establishment
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
                        using (var cts = new CancellationTokenSource(OnRemoteIdleTimeout))
                        {
                            if (_channel is IClientChannel)
                            {
                                await OnClientRemoteIdleAsync((IClientChannel)_channel, cts.Token).ConfigureAwait(false);

                            }
                            else if (_channel is IServerChannel)
                            {
                                await OnServerRemoteIdleAsync((IServerChannel)_channel, cts.Token).ConfigureAwait(false);
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

                        await _channel.SendCommandAsync(pingCommandRequest).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    if (!_cancellationTokenSource.IsCancellationRequested) throw;
                    break;
                }
            }
        }

        private static async Task OnClientRemoteIdleAsync(IClientChannel clientChannel, CancellationToken cancellationToken)
        {
            var receivedFinishedSessionTask = clientChannel.ReceiveFinishedSessionAsync(cancellationToken);
            await clientChannel.SendFinishingSessionAsync().ConfigureAwait(false);
            await receivedFinishedSessionTask.ConfigureAwait(false);
        }

        private static Task OnServerRemoteIdleAsync(IServerChannel serverChannel, CancellationToken cancellationToken)
        {
            return serverChannel.SendFinishedSessionAsync();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
            _pingRemoteTask?.Dispose();
        }
    }
}
