using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Util;
using Moq;
using NUnit.Core;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Client
{
    [TestFixture]
    public class EstablishedClientChannelBuilderTests
    {
        private CancellationToken _cancellationToken;
        private Mock<ITransport> _transport;
        private Mock<IClientChannel> _clientChannel;
        private Mock<IClientChannelBuilder> _clientChannelBuilder;

        private Uri _serverUri;
        private Node _localNode;
        private Node _remoteNode;
        private Guid _sessionId;
        private Session _negotiatingSession;
        private Session _negotiatedSession;
        private Session _authenticatingSession;
        private Session _establishedSession;

        #region Scenario

        [SetUp]
        public void Setup()
        {                  
            _cancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
            _clientChannel = new Mock<IClientChannel>();
                        
            _clientChannelBuilder = new Mock<IClientChannelBuilder>();
            _clientChannelBuilder
                .Setup(b => b.BuildAsync(_cancellationToken))
                .ReturnsAsync(_clientChannel.Object);
            _serverUri = Dummy.CreateUri();
            _clientChannelBuilder
                .SetupGet(b => b.ServerUri)
                .Returns(_serverUri);
            _localNode = Dummy.CreateNode();
            _remoteNode = Dummy.CreateNode();
            _sessionId = Guid.NewGuid();
            _negotiatingSession = Dummy.CreateSession(SessionState.Negotiating);
            _negotiatingSession.CompressionOptions = new[]
            {
                SessionCompression.None,
            };
            _negotiatingSession.EncryptionOptions = new[]
            {
                SessionEncryption.None, SessionEncryption.TLS,
            };
            _negotiatingSession.Id = _sessionId;
            _negotiatedSession = Dummy.CreateSession(SessionState.Negotiating);
            _negotiatedSession.Id = _sessionId;
            _negotiatedSession.Compression = SessionCompression.None;
            _negotiatedSession.Encryption = SessionEncryption.TLS;
            _authenticatingSession = Dummy.CreateSession(SessionState.Authenticating);            
            _authenticatingSession.Id = _sessionId;
            _authenticatingSession.SchemeOptions = new[]
            {
                AuthenticationScheme.Plain,
                AuthenticationScheme.Guest,
                AuthenticationScheme.Key,
                AuthenticationScheme.Transport,
            };
            _establishedSession = Dummy.CreateSession(SessionState.Established);
            _establishedSession.Id = _sessionId;
            _establishedSession.From = _remoteNode;
            _establishedSession.To = _localNode;
            _clientChannel
                .Setup(c => c.StartNewSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_negotiatingSession);
            _clientChannel
                .Setup(c => c.NegotiateSessionAsync(It.IsAny<SessionCompression>(), It.IsAny<SessionEncryption>(), It.IsAny<CancellationToken>()))
                .Returns((SessionCompression compression, SessionEncryption encryption, CancellationToken cancellationToken) =>
                {
                    _negotiatedSession.Compression = compression;
                    _negotiatedSession.Encryption = encryption;
                    return _negotiatedSession.AsCompletedTask();
                });
            _clientChannel
                .Setup(c => c.ReceiveAuthenticatingSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_authenticatingSession);
            _clientChannel
                .Setup(c => c.AuthenticateSessionAsync(It.IsAny<Identity>(), It.IsAny<Authentication>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_establishedSession);
            _transport = new Mock<ITransport>();
            _transport
                .SetupGet(t => t.Compression)
                .Returns(() => SessionCompression.None);
            _transport
                .SetupGet(t => t.Encryption)
                .Returns(() => SessionEncryption.None);
            _clientChannel
                .SetupGet(c => c.Transport)
                .Returns(_transport.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _clientChannelBuilder = null;
            _clientChannel = null;
        }

        #endregion

        private EstablishedClientChannelBuilder GetTarget()
        {
            return new EstablishedClientChannelBuilder(_clientChannelBuilder.Object);
        }

        [Test]
        public async Task WithCompression_GZipCompression_EstablishesSessionWithSelectedOption()
        {
            // Arrange                        
            var compression = SessionCompression.GZip;
            var target = GetTarget();

            // Act            
            target.WithCompression(compression);
            var channel = await target.BuildAndEstablishAsync(_cancellationToken);

            // Assert
            _transport.Verify(t => t.SetCompressionAsync(compression, It.IsAny<CancellationToken>()), Times.Once);            
        }

        [Test]
        public async Task WithCompression_GZipCompressionSelector_EstablishesSessionWithSelectedOption()
        {
            // Arrange        
            var compression = SessionCompression.GZip;
            Func<SessionCompression[], SessionCompression> selector = (options) => compression;
            var target = GetTarget();

            // Act            
            target.WithCompression(selector);
            var channel = await target.BuildAndEstablishAsync(_cancellationToken);

            // Assert
            _transport.Verify(t => t.SetCompressionAsync(compression, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task WithEncryption_TLSEncryption_EstablishesSessionWithSelectedOption()
        {
            // Arrange                        
            var encryption = SessionEncryption.TLS;
            var target = GetTarget();

            // Act            
            target.WithEncryption(encryption);
            var channel = await target.BuildAndEstablishAsync(_cancellationToken);

            // Assert
            _transport.Verify(t => t.SetEncryptionAsync(encryption, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task WithEncryption_TLSEncryptionSelector_EstablishesSessionWithSelectedOption()
        {
            // Arrange        
            var encryption = SessionEncryption.TLS;
            Func<SessionEncryption[], SessionEncryption> selector = (options) => encryption;
            var target = GetTarget();

            // Act            
            target.WithEncryption(selector);
            var channel = await target.BuildAndEstablishAsync(_cancellationToken);

            // Assert
            _transport.Verify(t => t.SetEncryptionAsync(encryption, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task WithAuthentication_PlainAuthentication_EstablishesSessionWithSelectedOption()
        {
            // Arrange                        
            var authentication = Dummy.CreatePlainAuthentication();
            var target = GetTarget();

            // Act            
            target.WithAuthentication(authentication);
            var channel = await target.BuildAndEstablishAsync(_cancellationToken);

            // Assert
            _clientChannel.Verify(c => c.AuthenticateSessionAsync(It.IsAny<Identity>(), authentication, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task WithAuthentication_PlainAuthenticationSelector_EstablishesSessionWithSelectedOption()
        {
            // Arrange                        
            var authentication = Dummy.CreatePlainAuthentication();
            Func<AuthenticationScheme[], Authentication, Authentication> authenticator =
                (schemes, roundtrip) => authentication;
            var target = GetTarget();

            // Act            
            target.WithAuthentication(authenticator);
            var channel = await target.BuildAndEstablishAsync(_cancellationToken);

            // Assert
            _clientChannel.Verify(c => c.AuthenticateSessionAsync(It.IsAny<Identity>(), authentication, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
