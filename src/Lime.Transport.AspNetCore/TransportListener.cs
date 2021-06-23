using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using Lime.Protocol.Util;
using Microsoft.Extensions.Options;

namespace Lime.Transport.AspNetCore
{
    public sealed class TransportListener
    {
        private readonly IOptions<LimeOptions> _options;

        public TransportListener(IOptions<LimeOptions> options)
        {
            _options = options;
        }

        public async Task ListenAsync(ITransport transport, CancellationToken cancellationToken)
        {
            using var channel = await EstablishChannelAsync(transport, cancellationToken);

            if (channel.State == SessionState.Established)
            {
                var listener = new ChannelListener(
                    (m, ct) => TaskUtil.TrueCompletedTask,
                    (n, ct) => TaskUtil.TrueCompletedTask,
                    (c, ct) => TaskUtil.TrueCompletedTask);
                listener.Start(channel);

                var sessionTask = channel.ReceiveFinishingSessionAsync(cancellationToken);

                await Task.WhenAny(
                    sessionTask,
                    listener.CommandListenerTask,
                    listener.MessageListenerTask,
                    listener.NotificationListenerTask);

                if (sessionTask.IsCompleted &&
                    channel.Transport.IsConnected)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await channel.SendFinishedSessionAsync(cts.Token);
                }
            }

            if (channel.State != SessionState.Finished &&
                channel.State != SessionState.Failed &&
                channel.Transport.IsConnected)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await channel.SendFailedSessionAsync(
                    new Reason()
                    {
                        Code = ReasonCodes.SESSION_ERROR,
                        Description = "The session was finished by the server"
                    },
                    cts.Token);
            }
        }

        private async Task<ServerChannel> EstablishChannelAsync(ITransport transport, CancellationToken cancellationToken)
        {
            var channel = new ServerChannel(
                EnvelopeId.NewId(),
                _options.Value.LocalNode,
                transport,
                _options.Value.SendTimeout);

            await channel.EstablishSessionAsync(
                transport.GetSupportedCompression().Intersect(_options.Value.EnabledCompressionOptions).ToArray(),
                transport.GetSupportedEncryption().Intersect(_options.Value.EnabledEncryptionOptions).ToArray(),
                _options.Value.SchemeOptions,
                (identity, authentication, c) => _options.Value.AuthenticationHandler(identity, authentication, c),
                (node, serverChannel, c) => _options.Value.RegistrationHandler(node, serverChannel, c),
                cancellationToken);

            return channel;
        }
    }
}