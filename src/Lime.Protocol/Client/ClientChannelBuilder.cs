using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;

namespace Lime.Protocol.Client
{
    /// <summary>
    /// Helper class for building instances of <see cref="ClientChannel"/>.
    /// </summary>
    public sealed class ClientChannelBuilder
    {
        private readonly Func<ITransport> _transportFactory;
        private readonly Uri _serverUri;
        private readonly List<IChannelModule<Message>> _messageChannelModules;
        private readonly List<IChannelModule<Notification>> _notificationChannelModules;
        private readonly List<IChannelModule<Command>> _commandChannelModules;
        private TimeSpan _sendTimeout;
        private int _buffersLimit;
        
        private ClientChannelBuilder(Func<ITransport> transportFactory, Uri serverUri)
        {
            if (transportFactory == null) throw new ArgumentNullException(nameof(transportFactory));
            if (serverUri == null) throw new ArgumentNullException(nameof(serverUri));
            _transportFactory = transportFactory;
            _serverUri = serverUri;
            _messageChannelModules = new List<IChannelModule<Message>>();
            _notificationChannelModules = new List<IChannelModule<Notification>>();
            _commandChannelModules = new List<IChannelModule<Command>>();

            _sendTimeout = TimeSpan.FromSeconds(60);
            _buffersLimit = 5;
        }

        /// <summary>
        /// Creates an instance of <see cref="ClientChannelBuilder"/> using the specified transport type.
        /// </summary>
        /// <typeparam name="TTransport">The type of the transport.</typeparam>
        /// <param name="serverUri">The server URI.</param>
        /// <returns></returns>
        public static ClientChannelBuilder Create<TTransport>(Uri serverUri) where TTransport : ITransport, new()
        {
            return Create(() => new TTransport(), serverUri);
        }

        /// <summary>
        /// Creates an instance of <see cref="ClientChannelBuilder"/> using the specified transport.
        /// </summary>
        /// <param name="transport">The transport.</param>
        /// <param name="serverUri">The server URI.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static ClientChannelBuilder Create(ITransport transport, Uri serverUri)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            return Create(() => transport, serverUri);
        }

        /// <summary>
        /// Creates an instance of <see cref="ClientChannelBuilder"/> using the specified transport factory.
        /// </summary>
        /// <param name="transportFactory">The transport factory.</param>
        /// <param name="serverUri">The server URI.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static ClientChannelBuilder Create(Func<ITransport> transportFactory, Uri serverUri)
        {
            if (transportFactory == null) throw new ArgumentNullException(nameof(transportFactory));
            return new ClientChannelBuilder(transportFactory, serverUri);            
        }

        /// <summary>
        /// Sets the send timeout.
        /// </summary>
        /// <param name="sendTimeout">The send timeout.</param>
        /// <returns></returns>
        public ClientChannelBuilder WithSendTimeout(TimeSpan sendTimeout)
        {
            _sendTimeout = sendTimeout;
            return this;
        }

        /// <summary>
        /// Sets the buffers limit.
        /// </summary>
        /// <param name="buffersLimit">The buffers limit.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public ClientChannelBuilder WithBuffersLimit(int buffersLimit)
        {
            if (buffersLimit <= 0) throw new ArgumentOutOfRangeException(nameof(buffersLimit));
            _buffersLimit = buffersLimit;
            return this;
        }

        /// <summary>
        /// Adds a message module to the channel.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns></returns>
        public ClientChannelBuilder AddMessageModule(IChannelModule<Message> module)
        {
            _messageChannelModules.Add(module);
            return this;
        }

        /// <summary>
        /// Adds a notification module to the channel.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns></returns>
        public ClientChannelBuilder AddNotificationModule(IChannelModule<Notification> module)
        {
            _notificationChannelModules.Add(module);
            return this;
        }

        /// <summary>
        /// Adds a command module to the channel.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns></returns>
        public ClientChannelBuilder AddCommandModule(IChannelModule<Command> module)
        {
            _commandChannelModules.Add(module);
            return this;
        }

        /// <summary>
        /// Builds a <see cref="ClientChannel"/> instance connecting the transport.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<IClientChannel> BuildAsync(CancellationToken cancellationToken)
        {
            var transport = _transportFactory();
            if (!transport.IsConnected)
            {
                await transport.OpenAsync(_serverUri, cancellationToken).ConfigureAwait(false);
            }

            var channel = new ClientChannel(
                transport,
                _sendTimeout,
                _buffersLimit);

            foreach (var module in _messageChannelModules)
            {
                channel.MessageModules.Add(module);
            }

            foreach (var module in _notificationChannelModules)
            {
                channel.NotificationModules.Add(module);
            }

            foreach (var module in _commandChannelModules)
            {
                channel.CommandModules.Add(module);
            }

            return channel;
        }

        /// <summary>
        /// Creates an <see cref="EstablishedClientChannelBuilder"/> to allow building and establishment of <see cref="ClientChannel"/> instances.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EstablishedClientChannelBuilder CreateEstablishedClientChannelBuilder(Identity identity)
        {
            if (identity == null) throw new ArgumentNullException(nameof(identity));
            return new EstablishedClientChannelBuilder(this, identity);
        }
    }
}