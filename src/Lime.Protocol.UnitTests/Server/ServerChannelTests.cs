using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using System.Threading.Tasks;
using System.Threading;
using Lime.Protocol.Security;

namespace Lime.Protocol.UnitTests.Server
{
    [TestClass]
    public class ServerChannelTests
    {
        private Mock<ITransport> _transport;
        private TimeSpan _sendTimeout;
        private Guid _sessionId;
        private Node _serverNode;

        #region Constructor

        public ServerChannelTests()
        {
            _transport = new Mock<ITransport>();
            _sendTimeout = TimeSpan.FromSeconds(30);
            _sessionId = Guid.NewGuid();
            _serverNode = DataUtil.CreateNode();
        }

        public ServerChannel GetTarget()
        {
            return new ServerChannel(
                _sessionId,
                _serverNode,
                _transport.Object,
                _sendTimeout);
        }

        #endregion

        #region SendNegotiatingSessionAsync
        [TestMethod]
        [TestCategory("SendNegotiatingSessionAsync")]
        public async Task SendNegotiatingSessionAsync_ValidOptions_CallsTransport()
        {
            var target = GetTarget();

            var compressionOptions = new SessionCompression[] { SessionCompression.None };
            var encryptionOptions = new SessionEncryption[] { SessionEncryption.None, SessionEncryption.TLS };

            await target.SendNegotiatingSessionAsync(compressionOptions, encryptionOptions);

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Negotiating &&
                                        e.CompressionOptions == compressionOptions &&
                                        e.EncryptionOptions == encryptionOptions &&
                                        e.From.Equals(target.LocalNode) &&
                                        e.To == null &&
                                        e.SchemeOptions == null &&
                                        e.Compression == null &&
                                        e.Encryption == null &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [TestCategory("SendNegotiatingSessionAsync")]
        public async Task SendNegotiatingSessionAsync_NullCompressionOptions_ThrowsArgumentNullException()
        {
            var target = GetTarget();

            SessionCompression[] compressionOptions = null;
            var encryptionOptions = new SessionEncryption[] { SessionEncryption.None, SessionEncryption.TLS };

            await target.SendNegotiatingSessionAsync(compressionOptions, encryptionOptions);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        [TestCategory("SendNegotiatingSessionAsync")]
        public async Task SendNegotiatingSessionAsync_EmptyCompressionOptions_ThrowsArgumentException()
        {
            var target = GetTarget();

            var compressionOptions = new SessionCompression[0];
            var encryptionOptions = new SessionEncryption[] { SessionEncryption.None, SessionEncryption.TLS };

            await target.SendNegotiatingSessionAsync(compressionOptions, encryptionOptions);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [TestCategory("SendNegotiatingSessionAsync")]
        public async Task SendNegotiatingSessionAsync_NullEncryptionOptions_ThrowsArgumentNullException()
        {
            var target = GetTarget();

            var compressionOptions = new SessionCompression[] { SessionCompression.None };
            SessionEncryption[] encryptionOptions = null;

            await target.SendNegotiatingSessionAsync(compressionOptions, encryptionOptions);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        [TestCategory("SendNegotiatingSessionAsync")]
        public async Task SendNegotiatingSessionAsync_EmptyEncryptionOptions_ThrowsArgumentException()
        {
            var target = GetTarget();

            var compressionOptions = new SessionCompression[] { SessionCompression.None };
            var encryptionOptions = new SessionEncryption[0];

            await target.SendNegotiatingSessionAsync(compressionOptions, encryptionOptions);
        } 
        #endregion


        #region SendAuthenticatingSessionAsync
        [TestMethod]
        [TestCategory("SendAuthenticatingSessionAsync")]
        public async Task SendAuthenticatingSessionAsync_ValidOptions_CallsTransport()
        {
            var target = GetTarget();

            AuthenticationScheme[] schemeOptions = new AuthenticationScheme[] { AuthenticationScheme.Guest, AuthenticationScheme.Plain };

            await target.SendAuthenticatingSessionAsync(schemeOptions);

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Authenticating &&
                                        e.SchemeOptions == schemeOptions &&
                                        e.From.Equals(target.LocalNode) &&
                                        e.To == null &&
                                        e.CompressionOptions == null &&
                                        e.Compression == null &&
                                        e.EncryptionOptions == null &&
                                        e.Encryption == null &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [TestCategory("SendAuthenticatingSessionAsync")]
        public async Task SendAuthenticatingSessionAsync_NullOptions_ThrowsArgumentNullException()
        {
            var target = GetTarget();

            AuthenticationScheme[] schemeOptions = null;

            await target.SendAuthenticatingSessionAsync(schemeOptions);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        [TestCategory("SendAuthenticatingSessionAsync")]
        public async Task SendAuthenticatingSessionAsync_EmptyOptions_ThrowsArgumentException()
        {
            var target = GetTarget();

            AuthenticationScheme[] schemeOptions = new AuthenticationScheme[0];

            await target.SendAuthenticatingSessionAsync(schemeOptions);
        } 

        #endregion
    }
}
