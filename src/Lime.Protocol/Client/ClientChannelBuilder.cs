using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;

namespace Lime.Protocol.Client
{
    /// <summary>
    /// Helper class for building instances of <see cref="ClientChannel"/>.
    /// </summary>
    public sealed class ClientChannelBuilder : IClientChannelBuilder
    {
        private readonly Func<ITransport> _transportFactory;        
        private readonly List<Func<IClientChannel, IChannelModule<Message>>> _messageChannelModules;
        private readonly List<Func<IClientChannel, IChannelModule<Notification>>> _notificationChannelModules;
        private readonly List<Func<IClientChannel, IChannelModule<Command>>> _commandChannelModules;
        private readonly List<Func<IClientChannel, CancellationToken, Task>> _builtHandlers;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientChannelBuilder"/> class.
        /// </summary>
        /// <param name="transportFactory">The transport factory.</param>
        /// <param name="serverUri">The server URI.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        private ClientChannelBuilder(Func<ITransport> transportFactory, Uri serverUri)
        {
            if (transportFactory == null) throw new ArgumentNullException(nameof(transportFactory));
            if (serverUri == null) throw new ArgumentNullException(nameof(serverUri));
            _transportFactory = transportFactory;
            ServerUri = serverUri;
            _messageChannelModules = new List<Func<IClientChannel, IChannelModule<Message>>>();
            _notificationChannelModules = new List<Func<IClientChannel, IChannelModule<Notification>>>();
            _commandChannelModules = new List<Func<IClientChannel, IChannelModule<Command>>>();
            _builtHandlers = new List<Func<IClientChannel, CancellationToken, Task>>();
            SendTimeout = TimeSpan.FromSeconds(60);
            BuffersLimit = 5;
        }

        /// <summary>
        /// Gets the server URI.
        /// </summary>
        public Uri ServerUri { get; }

        /// <summary>
        /// Gets the send timeout.
        /// </summary>        
        public TimeSpan SendTimeout { get; private set; }

        /// <summary>
        /// Gets the buffers limit.
        /// </summary>        
        public int BuffersLimit { get; private set; }

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
            SendTimeout = sendTimeout;
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
            BuffersLimit = buffersLimit;
            return this;
        }

        /// <summary>
        /// Adds a message module to the channel.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns></returns>
        public ClientChannelBuilder AddMessageModule(IChannelModule<Message> module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            return AddMessageModule(c => module);
        }

        /// <summary>
        /// Adds a message module to the channel.
        /// </summary>
        /// <param name="moduleFactory">The module factory.</param>
        /// <returns></returns>
        public ClientChannelBuilder AddMessageModule(Func<IClientChannel, IChannelModule<Message>> moduleFactory)
        {            
            _messageChannelModules.Add(moduleFactory);
            return this;
        }

        /// <summary>
        /// Adds a notification module to the channel.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns></returns>
        public ClientChannelBuilder AddNotificationModule(IChannelModule<Notification> module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            return AddNotificationModule(c => module);
        }

        /// <summary>
        /// Adds a notification module to the channel.
        /// </summary>
        /// <param name="moduleFactory">The module factory.</param>
        /// <returns></returns>
        public ClientChannelBuilder AddNotificationModule(Func<IClientChannel, IChannelModule<Notification>> moduleFactory)
        {
            if (moduleFactory == null) throw new ArgumentNullException(nameof(moduleFactory));
            _notificationChannelModules.Add(moduleFactory);
            return this;
        }

        /// <summary>
        /// Adds a command module to the channel.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns></returns>
        public ClientChannelBuilder AddCommandModule(IChannelModule<Command> module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));            
            return AddCommandModule(c => module);
        }

        /// <summary>
        /// Adds a command module to the channel.
        /// </summary>
        /// <param name="moduleFactory">The module factory.</param>
        /// <returns></returns>
        public ClientChannelBuilder AddCommandModule(Func<IClientChannel, IChannelModule<Command>> moduleFactory)
        {
            if (moduleFactory == null) throw new ArgumentNullException(nameof(moduleFactory));
            _commandChannelModules.Add(moduleFactory);
            return this;
        }

        /// <summary>
        /// Adds a handler to be executed after the channel is built.
        /// </summary>
        /// <param name="builtHandler">The handler to be executed.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public ClientChannelBuilder AddBuiltHandler(Func<IClientChannel, CancellationToken, Task> builtHandler)
        {
            if (builtHandler == null) throw new ArgumentNullException(nameof(builtHandler));
            _builtHandlers.Add(builtHandler);
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
                await transport.OpenAsync(ServerUri, cancellationToken).ConfigureAwait(false);
            }

            var clientChannel = new ClientChannel(
                transport,
                SendTimeout,
                BuffersLimit,
                autoReplyPings: false);

            try
            {
                foreach (var moduleFactory in _messageChannelModules.ToList())
                {
                    clientChannel.MessageModules.Add(moduleFactory(clientChannel));
                }

                foreach (var moduleFactory in _notificationChannelModules.ToList())
                {
                    clientChannel.NotificationModules.Add(moduleFactory(clientChannel));
                }

                foreach (var moduleFactory in _commandChannelModules.ToList())
                {
                    clientChannel.CommandModules.Add(moduleFactory(clientChannel));
                }

                foreach (var handler in _builtHandlers.ToList())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await handler(clientChannel, cancellationToken).ConfigureAwait(false);
                }
            }
            catch
            {
                clientChannel.DisposeIfDisposable();
                throw;
            }
            return clientChannel;
        }

        /// <summary>
        /// Creates an <see cref="EstablishedClientChannelBuilder"/> to allow building and establishment of <see cref="ClientChannel"/> instances.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public EstablishedClientChannelBuilder CreateEstablishedClientChannelBuilder()
        {
            return new EstablishedClientChannelBuilder(this);
        }
    }
}