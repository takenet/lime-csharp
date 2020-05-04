using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Util;

namespace Lime.Protocol.Server
{
    public class ServerBuilder
    {
        public ServerBuilder(Node serverNode, ITransportListener transportListener)
        {
            ServerNode = serverNode ?? throw new ArgumentNullException(nameof(serverNode));
            TransportListener = transportListener ?? throw new ArgumentNullException(nameof(transportListener));
            Authenticator = (node, authentication, cancellationToken) => Task.FromResult(
                new AuthenticationResult(DomainRole.Member, new Node(Guid.NewGuid().ToString(), ServerNode.Domain, "default")));
            ServerChannelFactory = transport => 
                new ServerChannel(
                    Guid.NewGuid().ToString(),
                    ServerNode,
                    transport,
                    TimeSpan.FromSeconds(30),
                    EnvelopeBufferSize);
            ChannelListenerFactory = _ => new ChannelListener(m => TaskUtil.TrueCompletedTask,
                n => TaskUtil.TrueCompletedTask, c => TaskUtil.TrueCompletedTask);
            EnvelopeBufferSize = 1;
        }

        public Node ServerNode { get; }

        public ITransportListener TransportListener { get; }

        public Func<ITransport, IServerChannel> ServerChannelFactory { get; private set; }

        public SessionCompression[] EnabledCompressionOptions { get; private set; } = {SessionCompression.None};

        public SessionEncryption[] EnabledEncryptionOptions { get; private set; } = {SessionEncryption.None};

        public AuthenticationScheme[] SchemeOptions { get; private set; } = {AuthenticationScheme.Guest};

        public Func<Identity, Authentication, CancellationToken, Task<AuthenticationResult>> Authenticator { get; private set; }
        
        public INodeRegistry NodeRegistry { get; private set; }

        public Func<IChannelInformation, IChannelListener> ChannelListenerFactory { get; private set; }

        public Func<Exception, Task<bool>> ExceptionHandler { get; private set; }

        public int MaxActiveChannels { get; private set; } = -1;

        public int EnvelopeBufferSize { get; private set; }
        
        public ServerBuilder WithServerChannelFactory(Func<ITransport, IServerChannel> serverChannelFactory)
        {
            ServerChannelFactory = serverChannelFactory ?? throw new ArgumentNullException(nameof(serverChannelFactory));
            return this;
        }

        public ServerBuilder WithEnabledCompressionOptions(SessionCompression[] enabledCompressionOptions)
        {
            EnabledCompressionOptions = enabledCompressionOptions ?? throw new ArgumentNullException(nameof(enabledCompressionOptions));
            return this;
        }

        public ServerBuilder WithEnabledEncryptionOptions(SessionEncryption[] enabledEncryptionOptions)
        {
            EnabledEncryptionOptions = enabledEncryptionOptions ?? throw new ArgumentNullException(nameof(enabledEncryptionOptions));
            return this;
        }

        public ServerBuilder WithEnabledSchemeOptions(AuthenticationScheme[] schemeOptions)
        {
            SchemeOptions = schemeOptions ?? throw new ArgumentNullException(nameof(schemeOptions));
            return this;
        }

        public ServerBuilder WithAuthenticator(Func<Identity, Authentication, CancellationToken, Task<AuthenticationResult>> authenticator)
        {
            Authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            return this;
        }
        
        public ServerBuilder WithNodeRegistry(INodeRegistry nodeRegistry)
        {
            NodeRegistry = nodeRegistry ?? throw new ArgumentNullException(nameof(nodeRegistry));
            return this;
        }

        public ServerBuilder WithChannelListenerFactory(Func<IChannelInformation, IChannelListener> channelListenerFactory)
        {
            ChannelListenerFactory = channelListenerFactory ?? throw new ArgumentNullException(nameof(channelListenerFactory));
            return this;
        }

        public ServerBuilder WithChannelConsumers(Func<Message, Task<bool>> messageConsumer, Func<Notification, Task<bool>> notificationConsumer, Func<Command, Task<bool>> commandConsumer)
        {
            if (messageConsumer == null) throw new ArgumentNullException(nameof(messageConsumer));
            if (notificationConsumer == null) throw new ArgumentNullException(nameof(notificationConsumer));
            if (commandConsumer == null) throw new ArgumentNullException(nameof(commandConsumer));
            return WithChannelListenerFactory(_ => new ChannelListener(messageConsumer, notificationConsumer, commandConsumer));
        }

        public ServerBuilder WithExceptionHandler(Func<Exception, Task<bool>> exceptionHandler)
        {
            ExceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            return this;
        }

        public ServerBuilder WithMaxActiveChannels(int maxActiveChannels)
        {
            if (maxActiveChannels == 0) throw new ArgumentOutOfRangeException(nameof(maxActiveChannels));
            MaxActiveChannels = maxActiveChannels;
            return this;
        }
        
        public ServerBuilder WithEnvelopeBufferSize(int envelopeBufferSize)
        {
            EnvelopeBufferSize = envelopeBufferSize;
            return this;
        }
        
        public IServer Build()
        {
            return new Server(
                TransportListener,
                ServerChannelFactory,
                EnabledCompressionOptions,
                EnabledEncryptionOptions,
                SchemeOptions,
                Authenticator,
                ChannelListenerFactory,
                NodeRegistry,
                ExceptionHandler,
                MaxActiveChannels);
        }
    }
}
