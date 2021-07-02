using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using Lime.Transport.AspNetCore.Listeners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lime.Transport.AspNetCore.Transport
{
    internal sealed class TransportListener
    {
        private readonly IOptions<LimeOptions> _options;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<TransportListener> _logger;
        private readonly ConcurrentDictionary<Node, ISenderChannel> _establishedChannels;

        public TransportListener(
            IOptions<LimeOptions> options,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<TransportListener> logger)
        {
            _options = options;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _establishedChannels = new ConcurrentDictionary<Node, ISenderChannel>();
        }

        public async Task ListenAsync(ITransport transport, CancellationToken cancellationToken)
        {
            using var channel = CreateChannel(transport);
            await EstablishChannelAsync(channel, cancellationToken);

            if (channel.State == SessionState.Established)
            {
                await ListenChannelAsync(channel, cancellationToken);
            }

            await CloseChannelAsync(channel);
        }

        private ServerChannel CreateChannel(ITransport transport) =>
            new ServerChannel(
                EnvelopeId.NewId(),
                _options.Value.LocalNode,
                transport,
                _options.Value.SendTimeout,
                _options.Value.EnvelopeBufferSize,
                _options.Value.FillEnvelopeRecipients,
                _options.Value.AutoReplyPings,
                _options.Value.RemotePingInterval,
                _options.Value.RemoteIdleTimeout,
                _options.Value.ConsumeTimeout,
                _options.Value.CloseTimeout);

        private async Task EstablishChannelAsync(ServerChannel channel, CancellationToken cancellationToken)
        {
            var compressionOptions = channel
                .Transport
                .GetSupportedCompression()
                .Intersect(_options.Value.EnabledCompressionOptions)
                .ToArray();

            var encryptionOptions = channel
                .Transport
                .GetSupportedEncryption()
                .Intersect(_options.Value.EnabledEncryptionOptions)
                .ToArray();

            await channel.EstablishSessionAsync(
                compressionOptions,
                encryptionOptions,
                _options.Value.SchemeOptions,
                (identity, authentication, c) => _options.Value.AuthenticationHandler(identity, authentication, c),
                (node, serverChannel, c) => _options.Value.RegistrationHandler(node, serverChannel, c),
                cancellationToken);
        }

        private ChannelListener CreateChannelListener(SenderChannelAdapter senderChannel) =>
            new ChannelListener(
                (m, ct) => OnMessageAsync(m, senderChannel, ct),
                (n, ct) => OnNotificationAsync(n, senderChannel, ct),
                (c, ct) => OnCommandAsync(c, senderChannel, ct));

        public async Task<bool> OnCommandAsync(Command command, ISenderChannel channel, CancellationToken cancellationToken)
        {
            using var _ = _logger.BeginScope(
                new Dictionary<string, string>
                {
                    {"Command.Id", command.Id},
                    {"Command.From", command.From},
                    {"Command.To", command.To},
                    {"Command.Method", command.Method.ToString()},
                    {"Command.Uri", command.Uri},
                    {"Command.Type", command.Type},
                });

            await InvokeListenersAsync<ICommandListener, Command>(
                command,
                channel,
                cancellationToken);

            return true;
        }

        public async Task<bool> OnNotificationAsync(Notification notification, ISenderChannel channel, CancellationToken cancellationToken)
        {
            using var _ = _logger.BeginScope(
                new Dictionary<string, string>
                {
                    {"id", notification.Id},
                    {"from", notification.From},
                    {"to", notification.To},
                    {"event", notification.Event.ToString()},
                });

            await InvokeListenersAsync<INotificationListener, Notification>(
                notification,
                channel,
                cancellationToken);

            return true;
        }

        public async Task<bool> OnMessageAsync(Message message, ISenderChannel channel, CancellationToken cancellationToken)
        {
            using var _ = _logger.BeginScope(
                new Dictionary<string, string>
                {
                    {"id", message.Id},
                    {"from", message.From},
                    {"to", message.To},
                    {"type", message.Type},
                });

            await InvokeListenersAsync<IMessageListener, Message>(
                message,
                channel,
                cancellationToken);

            return true;
        }

        private async Task<int> InvokeListenersAsync<TListener, TEnvelope>(
            TEnvelope envelope,
            ISenderChannel channel,
            CancellationToken cancellationToken)
            where TEnvelope : Envelope, new()
            where TListener : IEnvelopeListener<TEnvelope>
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var channelContext = new ChannelContext(channel, GetChannel);
            var contextProvider = scope.ServiceProvider.GetRequiredService<ChannelContextProvider>();
            contextProvider.SetContext(channelContext);

            try
            {
                var listeners = scope
                    .ServiceProvider
                    .GetServices<TListener>()
                    ?.Where(l => l.Filter(envelope))
                    .ToArray();

                if (listeners != null && listeners.Length > 0)
                {
                    await Task.WhenAll(
                        listeners
                            .Select(l =>
                                Task.Run(() => l.OnEnvelopeAsync(envelope, cancellationToken), cancellationToken)));

                    return listeners.Length;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{EnvelopeType} processing failed", typeof(TEnvelope).Name);
            }

            return 0;
        }

        private ISenderChannel? GetChannel(Node node) => _establishedChannels.TryGetValue(node, out var c) ? c : null;

        private async Task ListenChannelAsync(IServerChannel channel, CancellationToken cancellationToken)
        {
            var node = channel.RemoteNode;
            var senderChannel = new SenderChannelAdapter(channel);
            _establishedChannels[node] = senderChannel;

            using var listener = CreateChannelListener(senderChannel);

            var sessionTask = channel.ReceiveFinishingSessionAsync(cancellationToken);

            listener.Start(channel);

            try
            {
                await Task.WhenAny(
                    sessionTask,
                    listener.CommandListenerTask,
                    listener.MessageListenerTask,
                    listener.NotificationListenerTask);
            }
            finally
            {
                listener.Stop();
                _establishedChannels.TryRemove(node, out _);
            }

            if (sessionTask.IsCompleted &&
                channel.Transport.IsConnected)
            {
                using var cts = new CancellationTokenSource(_options.Value.CloseTimeout);
                await channel.SendFinishedSessionAsync(cts.Token);
            }
        }

        private async Task CloseChannelAsync(IServerChannel channel)
        {
            if (channel.State != SessionState.Finished &&
                channel.State != SessionState.Failed &&
                channel.Transport.IsConnected)
            {
                using var cts = new CancellationTokenSource(_options.Value.CloseTimeout);
                await channel.SendFailedSessionAsync(
                    new Reason()
                    {
                        Code = ReasonCodes.SESSION_ERROR,
                        Description = "The session was finished by the server"
                    },
                    cts.Token);
            }
        }
    }
}