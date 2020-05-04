using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Server;
using Moq;
using NUnit.Framework;

namespace Lime.Protocol.UnitTests.Server
{
    [TestFixture]
    public class ServerTests
    {
        private Mock<ITransportListener> _transportListener;
        private Mock<IChannelListener> _channelListener;
        private INodeRegistry _nodeRegistry;
        private Node _localNode;
        private TimeSpan _sendTimeout;
        private SessionCompression[] _enabledCompressionOptions;
        private SessionEncryption[] _enabledEncryptionOptions;
        private AuthenticationScheme[] _schemeOptions;
        private Func<ITransport, IServerChannel> _serverChannelFactory;
        private Func<Identity, Authentication, CancellationToken, Task<AuthenticationResult>> _authenticator;
        private Func<IChannelInformation, IChannelListener> _channelListenerFactory;
        

        [SetUp]
        public void SetUp()
        {
            _transportListener = new Mock<ITransportListener>();
            _channelListener = new Mock<IChannelListener>();
            _nodeRegistry = new NodeRegistry();

            _localNode = Dummy.CreateNode();
            _sendTimeout = TimeSpan.FromSeconds(30);
            _enabledCompressionOptions = new [] {SessionCompression.None};
            _enabledEncryptionOptions = new [] {SessionEncryption.None, SessionEncryption.TLS};
            _schemeOptions = new[] {AuthenticationScheme.Guest, AuthenticationScheme.Plain, AuthenticationScheme.Transport};
            
            _serverChannelFactory = t => new ServerChannel(EnvelopeId.NewId(), _localNode, t, _sendTimeout);
            _authenticator = (node, authentication, cancellationToken) => Task.FromResult(new AuthenticationResult(DomainRole.Member));
            _channelListenerFactory = _ => _channelListener.Object;
        }
        
        private Protocol.Server.Server GetTarget()
        {
            return new Protocol.Server.Server(
                _transportListener.Object,
                _serverChannelFactory,
                _enabledCompressionOptions,
                _enabledEncryptionOptions,
                _schemeOptions,
                _authenticator,
                _channelListenerFactory,
                _nodeRegistry);
        }
        
        
        
    }
}