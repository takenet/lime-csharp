using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Util;
using Moq;
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
        private Mock<IDisposable> _disposableClientChannel;
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
            _disposableClientChannel = _clientChannel.As<IDisposable>();
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
        public void WithCompression_NullCompressionSelector_ThrowsArgumentNullException()
        {
            // Arrange                    
            Func<SessionCompression[], SessionCompression> selector = null;
            var target = GetTarget();

            // Act                        
            Action action = () => target.WithCompression(selector);
            action.ShouldThrow<ArgumentNullException>();
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
        public void WithEncryption_NullEncryptionSelector_ThrowsArgumentNullException()
        {
            // Arrange                    
            Func<SessionEncryption[], SessionEncryption> selector = null;
            var target = GetTarget();

            // Act            
            Action action = () => target.WithEncryption(selector);
            action.ShouldThrow<ArgumentNullException>();
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
        public async Task WithAuthentication_PlainAuthenticationAuthenticator_EstablishesSessionWithSelectedOption()
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

        [Test]
        public void WithAuthentication_NullAuthenticator_ThrowsArgumentNullException()
        {
            // Arrange                        
            Func<AuthenticationScheme[], Authentication, Authentication> authenticator =
                null;
            var target = GetTarget();

            // Act            
            Action action = () => target.WithAuthentication(authenticator);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public async Task WithPlainAuthentication_AnyPassword_EstablishesSessionWithSelectedOption()
        {
            // Arrange                        
            var password = Dummy.CreateRandomString(100);
            var target = GetTarget();

            // Act            
            target.WithPlainAuthentication(password);
            var channel = await target.BuildAndEstablishAsync(_cancellationToken);

            // Assert
            _clientChannel.Verify(c => c.AuthenticateSessionAsync(
                It.IsAny<Identity>(), 
                It.Is<Authentication>(a => a is PlainAuthentication && ((PlainAuthentication)a).GetFromBase64Password().Equals(password)), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Test]
        public void WithPlainAuthentication_NullPassword_ThrowsArgumentNullException()
        {
            // Arrange                        
            string password = null;
            var target = GetTarget();

            // Act            
            Action action = () => target.WithPlainAuthentication(password);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public async Task WithKeyAuthentication_AnyPassword_EstablishesSessionWithSelectedOption()
        {
            // Arrange                        
            var key = Dummy.CreateRandomString(100);
            var target = GetTarget();

            // Act            
            target.WithKeyAuthentication(key);
            var channel = await target.BuildAndEstablishAsync(_cancellationToken);

            // Assert
            _clientChannel.Verify(c => c.AuthenticateSessionAsync(
                It.IsAny<Identity>(), 
                It.Is<Authentication>(a => a is KeyAuthentication && ((KeyAuthentication)a).GetFromBase64Key().Equals(key)), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Test]
        public void WithKeyAuthentication_NullPassword_ThrowsArgumentNullException()
        {
            // Arrange                        
            string password = null;
            var target = GetTarget();

            // Act            
            Action action = () => target.WithKeyAuthentication(password);
            action.ShouldThrow<ArgumentNullException>();            
        }

        [Test]
        public async Task WithIdentity_AnyIdentity_EstablishesSessionWithSelectedOption()
        {
            // Arrange                        
            var identity = Dummy.CreateIdentity();
            var target = GetTarget();

            // Act            
            target.WithIdentity(identity);
            var channel = await target.BuildAndEstablishAsync(_cancellationToken);

            // Assert
            _clientChannel.Verify(c => c.AuthenticateSessionAsync(
                identity,
                It.IsAny<Authentication>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task WithInstance_AnyInstance_EstablishesSessionWithSelectedOption()
        {
            // Arrange                        
            var instance = Dummy.CreateRandomString(10);
            var target = GetTarget();

            // Act            
            target.WithInstance(instance);
            var channel = await target.BuildAndEstablishAsync(_cancellationToken);

            // Assert
            _clientChannel.Verify(c => c.AuthenticateSessionAsync(
                It.IsAny<Identity>(),
                It.IsAny<Authentication>(),
                instance,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task WithCompression_Encryption_Identity_Authentication_Instance_AnyOptions_EstablishesSessionWithSelectedOptions()
        {
            // Arrange                
            var compression = SessionCompression.GZip;
            var encryption = SessionEncryption.TLS;
            var identity = Dummy.CreateIdentity();
            var authentication = Dummy.CreatePlainAuthentication();            
            var instance = Dummy.CreateRandomString(10);
            var target = GetTarget();

            // Act            
            target
                .WithCompression(compression)
                .WithEncryption(encryption)
                .WithIdentity(identity)
                .WithAuthentication(authentication)
                .WithInstance(instance);
            var channel = await target.BuildAndEstablishAsync(_cancellationToken);

            // Assert
            _transport.Verify(t => t.SetCompressionAsync(compression, It.IsAny<CancellationToken>()), Times.Once);
            _transport.Verify(t => t.SetEncryptionAsync(encryption, It.IsAny<CancellationToken>()), Times.Once);
            _clientChannel.Verify(c => c.AuthenticateSessionAsync(
                identity,
                authentication,
                instance,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task AddEstablishedHandler_CallsBuildAsync_ShouldExecuteHandler()
        {
            // Arrange            
            _transport.Setup(t => t.IsConnected).Returns(true);
            IClientChannel handlerClientChannel = null;
            var handlerCancellationToken = default(CancellationToken);
            Func<IClientChannel, CancellationToken, Task> establishedHandler = (clientChannel, cancellationToken) =>
            {
                handlerClientChannel = clientChannel;
                handlerCancellationToken = cancellationToken;
                return TaskUtil.CompletedTask;
            };

            var target = GetTarget();

            // Act
            target.AddEstablishedHandler(establishedHandler);
            var channel = await target.BuildAndEstablishAsync(_cancellationToken);

            // Assert            
            handlerClientChannel.ShouldNotBeNull();
            handlerClientChannel.ShouldBe(channel);
            handlerCancellationToken.ShouldBe(_cancellationToken);
        }

        [Test]
        public async Task AddEstablishedHandler_HandlerThrowsException_ShouldDisposeChannelAndRethrowToCaller()
        {
            // Arrange            
            var exception = Dummy.CreateException<ApplicationException>();
            _transport.Setup(t => t.IsConnected).Returns(true);
            Func<IClientChannel, CancellationToken, Task> establishedHandler = (clientChannel, cancellationToken) =>
            {
                throw exception;
            };
            var target = GetTarget();

            // Act
            target.AddEstablishedHandler(establishedHandler);            
            await target
                .BuildAndEstablishAsync(_cancellationToken)
                .ShouldThrowAsync<ApplicationException>();            
            _disposableClientChannel.Verify(d => d.Dispose(), Times.Once);                            
        }

        [Test]
        public async Task AddEstablishedHandler_MultipleHandlers_ShouldExecuteHandlers()
        {
            // Arrange            
            _transport.Setup(t => t.IsConnected).Returns(true);
            var handlerClientChannels = new List<IClientChannel>();
            var handlerCancellationTokens = new List<CancellationToken>();
            var target = GetTarget();

            // Act
            var count = Dummy.CreateRandomInt(100);
            for (int i = 0; i < count; i++)
            {
                Func<IClientChannel, CancellationToken, Task> establishedHandler = (clientChannel, cancellationToken) =>
                {
                    handlerClientChannels.Add(clientChannel);
                    handlerCancellationTokens.Add(cancellationToken);
                    return TaskUtil.CompletedTask;
                };
                target.AddEstablishedHandler(establishedHandler);
            }
            var channel = await target.BuildAndEstablishAsync(_cancellationToken);

            // Assert            
            handlerClientChannels.Count.ShouldBe(count);
            handlerCancellationTokens.Count.ShouldBe(count);
            handlerClientChannels.ShouldAllBe(c => c == channel);
            handlerCancellationTokens.ShouldAllBe(t => t == _cancellationToken);
        }
    }
}
