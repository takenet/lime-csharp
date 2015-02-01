using System;
using NUnit.Framework;
using Moq;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using System.Threading.Tasks;
using System.Threading;
using Lime.Protocol.Security;

namespace Lime.Protocol.UnitTests.Server
{
    [TestFixture]
    public class ServerChannelTests
    {
        #region Private fields

        private Mock<ITransport> _transport;
        private TimeSpan _sendTimeout;

        #endregion

        #region Constructor

        public ServerChannelTests()
        {
            _transport = new Mock<ITransport>();
            _sendTimeout = TimeSpan.FromSeconds(30);
        }

        #endregion

        private ServerChannel GetTarget(SessionState state = SessionState.New, Node remoteNode = null, Guid sessionId = default(Guid), Node serverNode = null)
        {
            if (sessionId == Guid.Empty)
            {
                sessionId = Guid.NewGuid();
            }

            if (serverNode == null)
            {
                serverNode = DataUtil.CreateNode();
            }

            return new TestServerChannel(
                state,
                sessionId,
                serverNode,
                _transport.Object,
                _sendTimeout,
                remoteNode);
        }

        #region ReceiveNewSessionAsync

        [Test]
        [Category("ReceiveNewSessionAsync")]
        public async Task ReceiveNewSessionAsync_NewState_ReadsTransport()
        {
            var target = GetTarget(SessionState.New);

            var session = DataUtil.CreateSession();
            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actual = await target.ReceiveNewSessionAsync(cancellationToken);

            Assert.AreEqual(session, actual);
            _transport.Verify();
        }

        [Test]
        [Category("ReceiveNewSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveNewSessionAsync_NotNewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.Established);

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.ReceiveNewSessionAsync(cancellationToken);
        }

        #endregion

        #region NegotiateSessionAsync

        [Test]
        [Category("NegotiateSessionAsync")]
        public async Task NegotiateSessionAsync_NewStateValidOptions_CallsTransportAndReadsFromBuffer()
        {
            var session = DataUtil.CreateSession(SessionState.Negotiating);


            var target = GetTarget(sessionId: session.Id);           

            var compressionOptions = new SessionCompression[] { SessionCompression.None };
            var encryptionOptions = new SessionEncryption[] { SessionEncryption.None, SessionEncryption.TLS };

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actual = await target.NegotiateSessionAsync(compressionOptions, encryptionOptions, cancellationToken);

            _transport.Verify();

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
                                        e.Authentication == null &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            Assert.AreEqual(SessionState.Negotiating, target.State);
            Assert.AreEqual(session, actual);
        }

        [Test]
        [Category("NegotiateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task NegotiateSessionAsync_InvalidStateValidOptions_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.Negotiating);

            var compressionOptions = new SessionCompression[] { SessionCompression.None };
            var encryptionOptions = new SessionEncryption[] { SessionEncryption.None, SessionEncryption.TLS };

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.NegotiateSessionAsync(compressionOptions, encryptionOptions, cancellationToken);
        }

        [Test]
        [Category("NegotiateSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task NegotiateSessionAsync_NullCompressionOptions_ThrowsArgumentNullException()
        {
            var target = GetTarget();

            SessionCompression[] compressionOptions = null;
            var encryptionOptions = new SessionEncryption[] { SessionEncryption.None, SessionEncryption.TLS };

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.NegotiateSessionAsync(compressionOptions, encryptionOptions, cancellationToken);
        }

        [Test]
        [Category("NegotiateSessionAsync")]
        [ExpectedException(typeof(ArgumentException))]
        public async Task NegotiateSessionAsync_EmptyCompressionOptions_ThrowsArgumentNullException()
        {
            var target = GetTarget();

            var compressionOptions = new SessionCompression[0];
            var encryptionOptions = new SessionEncryption[] { SessionEncryption.None, SessionEncryption.TLS };

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.NegotiateSessionAsync(compressionOptions, encryptionOptions, cancellationToken);
        }

        [Test]
        [Category("NegotiateSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task NegotiateSessionAsync_NullEncryptionOptions_ThrowsArgumentNullException()
        {
            var target = GetTarget();

            var compressionOptions = new SessionCompression[] { SessionCompression.None };
            SessionEncryption[] encryptionOptions = null;

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.NegotiateSessionAsync(compressionOptions, encryptionOptions, cancellationToken);
        }

        [Test]
        [Category("NegotiateSessionAsync")]
        [ExpectedException(typeof(ArgumentException))]
        public async Task NegotiateSessionAsync_EmptyEncryptionOptions_ThrowsArgumentException()
        {
            var target = GetTarget();

            var compressionOptions = new SessionCompression[] { SessionCompression.None };
            var encryptionOptions = new SessionEncryption[0];

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.NegotiateSessionAsync(compressionOptions, encryptionOptions, cancellationToken);
        }

        #endregion

        #region SendNegotiatingSessionAsync

        [Test]
        [Category("SendNegotiatingSessionAsync")]
        public async Task SendNegotiatingSessionAsync_NegotiatingState_CallsTransport()
        {
            var target = GetTarget(SessionState.Negotiating);
            var sessionCompression = SessionCompression.GZip;
            var sessionEncryption = SessionEncryption.TLS;

            await target.SendNegotiatingSessionAsync(
                sessionCompression,
                sessionEncryption);

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Negotiating &&
                                        e.SchemeOptions == null &&
                                        e.From.Equals(target.LocalNode) &&
                                        e.To == null &&
                                        e.CompressionOptions == null &&
                                        e.Compression == sessionCompression &&
                                        e.EncryptionOptions == null &&
                                        e.Encryption == sessionEncryption &&
                                        e.Authentication == null &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        #endregion

        #region AuthenticateSessionAsync

        [Test]
        [Category("AuthenticateSessionAsync")]
        public async Task AuthenticateSessionAsync_NegotiatingStateValidOptions_CallsTransportAndReadsFromBuffer()
        {
            var session = DataUtil.CreateSession(SessionState.Authenticating);
                        
            var target = GetTarget(SessionState.Negotiating, sessionId: session.Id);

            var schemeOptions = DataUtil.CreateSchemeOptions();
           
            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actual = await target.AuthenticateSessionAsync(schemeOptions, cancellationToken);

            _transport.Verify();

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Authenticating &&
                                        e.CompressionOptions == null &&
                                        e.EncryptionOptions == null &&
                                        e.From.Equals(target.LocalNode) &&
                                        e.To == null &&
                                        e.SchemeOptions == schemeOptions &&
                                        e.Compression == null &&
                                        e.Encryption == null &&
                                        e.Authentication == null &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            Assert.AreEqual(SessionState.Authenticating, target.State);
            Assert.AreEqual(session, actual);
        }

        [Test]
        [Category("AuthenticateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task AuthenticateSessionAsync_InvalidStateValidOptions_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.Established);

            var schemeOptions = DataUtil.CreateSchemeOptions();            

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.AuthenticateSessionAsync(schemeOptions, cancellationToken);
        }

        [Test]
        [Category("AuthenticateSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AuthenticateSessionAsync_NullOptions_ThrowsArgumentNullException()
        {
            var target = GetTarget(SessionState.Negotiating);

            AuthenticationScheme[] schemeOptions = null;

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.AuthenticateSessionAsync(schemeOptions, cancellationToken);
        }

        [Test]
        [Category("AuthenticateSessionAsync")]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AuthenticateSessionAsync_EmptyOptions_ThrowsArgumentException()
        {
            var target = GetTarget(SessionState.Negotiating);

            AuthenticationScheme[] schemeOptions = new AuthenticationScheme[0];

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.AuthenticateSessionAsync(schemeOptions, cancellationToken);
        }

        [Test]
        [Category("AuthenticateSessionAsync")]
        public async Task AuthenticateSessionAsync_AuthenticatingStateValidRoundtrip_CallsTransportAndReadsFromBuffer()
        {
            var session = DataUtil.CreateSession(SessionState.Authenticating);

            var target = GetTarget(SessionState.Authenticating, sessionId: session.Id);

            var authenticationRoundtrip = DataUtil.CreatePlainAuthentication();
            
            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actual = await target.AuthenticateSessionAsync(authenticationRoundtrip, cancellationToken);

            _transport.Verify();

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Authenticating &&
                                        e.CompressionOptions == null &&
                                        e.EncryptionOptions == null &&
                                        e.From.Equals(target.LocalNode) &&
                                        e.To == null &&
                                        e.SchemeOptions == null &&
                                        e.Compression == null &&
                                        e.Encryption == null &&
                                        e.Authentication == authenticationRoundtrip &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            Assert.AreEqual(SessionState.Authenticating, target.State);
            Assert.AreEqual(session, actual);
        }

        [Test]
        [Category("AuthenticateSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AuthenticateSessionAsync_AuthenticatingStateNullRoundtrip_ThrowsArgumentNullException()
        {
            var target = GetTarget(SessionState.Authenticating);

            Authentication authenticationRoundtrip = null;

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.AuthenticateSessionAsync(authenticationRoundtrip, cancellationToken);
        }

        [Test]
        [Category("AuthenticateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task AuthenticateSessionAsync_InvalidStateValidRoundtrip_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var authenticationRoundtrip = DataUtil.CreatePlainAuthentication();

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.AuthenticateSessionAsync(authenticationRoundtrip, cancellationToken);
        }

        #endregion

        #region SendEstablishedSessionAsync

        [Test]
        [Category("SendEstablishedSessionAsync")]
        public async Task SendEstablishedSessionAsync_ValidArgumentsAuthenticatingState_CallsTransport()
        {
            var target = GetTarget(SessionState.Authenticating);

            var node = DataUtil.CreateNode();

            await target.SendEstablishedSessionAsync(node);

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Established &&
                                        e.Authentication == null &&
                                        e.SchemeOptions == null &&
                                        e.From.Equals(target.LocalNode) &&
                                        e.To.Equals(node) &&
                                        e.CompressionOptions == null &&
                                        e.Compression == null &&
                                        e.EncryptionOptions == null &&
                                        e.Encryption == null &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            Assert.AreEqual(target.State, SessionState.Established);
            Assert.AreEqual(target.RemoteNode, node);
        }

        [Test]
        [Category("SendEstablishedSessionAsync")]
        public async Task SendEstablishedSessionAsync_ValidArgumentsNewState_CallsTransport()
        {
            var target = GetTarget();

            var node = DataUtil.CreateNode();
            await target.SendEstablishedSessionAsync(node);

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Established &&
                                        e.Authentication == null &&
                                        e.SchemeOptions == null &&
                                        e.From.Equals(target.LocalNode) &&
                                        e.To.Equals(node) &&
                                        e.CompressionOptions == null &&
                                        e.Compression == null &&
                                        e.EncryptionOptions == null &&
                                        e.Encryption == null &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            Assert.AreEqual(target.State, SessionState.Established);
            Assert.AreEqual(target.RemoteNode, node);
        }        

        [Test]
        [Category("SendEstablishedSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendEstablishedSessionAsync_NullNodeAuthenticatingState_ThrowsArgumentNullException()
        {
            var target = GetTarget(SessionState.Authenticating);

            Node node = null;

            await target.SendEstablishedSessionAsync(node);
        }

        #endregion

        #region ReceiveFinishingSessionAsync

        [Test]
        [Category("ReceiveFinishingSessionAsync")]
        public async Task ReceiveFinishingSessionAsync_EstablishedState_ReadsTransport()
        {            
            var session = DataUtil.CreateSession(SessionState.Finishing);
            
            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(session))
                .Returns(tcs.Task);


            var target = GetTarget(SessionState.Established);
            session.Id = target.SessionId;

            var actual = await target.ReceiveFinishingSessionAsync(cancellationToken);

            Assert.AreEqual(session, actual);
            _transport.Verify();
        }

        [Test]
        [Category("ReceiveFinishingSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveFinishingSessionAsync_InvalidState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.Authenticating);

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.ReceiveFinishingSessionAsync(cancellationToken);
        }

        #endregion

        #region SendFinishedSessionAsync

        [Test]
        [Category("SendFinishedSessionAsync")]
        public async Task SendFinishedSessionAsync_EstablishedState_CallsAndClosesTransport()
        {
            var remoteNode = DataUtil.CreateNode();

            var target = GetTarget(SessionState.Established, remoteNode);

            await target.SendFinishedSessionAsync();

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Finished &&
                                        e.Id == target.SessionId &&
                                        e.From.Equals(target.LocalNode) &&
                                        e.To.Equals(target.RemoteNode) &&
                                        e.Authentication == null &&
                                        e.SchemeOptions == null &&
                                        e.CompressionOptions == null &&
                                        e.Compression == null &&
                                        e.EncryptionOptions == null &&
                                        e.Encryption == null),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            _transport.Verify(
                t => t.CloseAsync(
                    It.IsAny<CancellationToken>()));

            Assert.AreEqual(target.State, SessionState.Finished);
        }

        [Test]
        [Category("SendFinishedSessionAsync")]
        public async Task SendFinishedSessionAsync_NewState_ClosesTransport()
        {
            var target = GetTarget();
            await target.SendFinishedSessionAsync();

            _transport.Verify(
                t => t.CloseAsync(
                    It.IsAny<CancellationToken>()));

            Assert.AreEqual(target.State, SessionState.Finished);

        }

        #endregion

        #region SendFailedSessionAsync

        [Test]
        [Category("SendFailedSessionAsync")]
        public async Task SendFailedSessionAsync_EstablishedState_CallsAndClosesTransport()
        {
            var remoteNode = DataUtil.CreateNode();

            var target = GetTarget(SessionState.Established, remoteNode);

            var reason = DataUtil.CreateReason();

            await target.SendFailedSessionAsync(reason);

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Failed &&
                                        e.Id == target.SessionId &&
                                        e.From.Equals(target.LocalNode) &&
                                        e.To.Equals(target.RemoteNode) &&
                                        e.Authentication == null &&
                                        e.SchemeOptions == null &&
                                        e.CompressionOptions == null &&
                                        e.Compression == null &&
                                        e.EncryptionOptions == null &&
                                        e.Encryption == null),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            _transport.Verify(
                t => t.CloseAsync(
                    It.IsAny<CancellationToken>()));

            Assert.AreEqual(target.State, SessionState.Failed);
        }

        [Test]
        [Category("SendFailedSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendFailedSessionAsync_NullReason_ThrowsArgumentNullException()
        {
            var target = GetTarget();
            Reason reason = null;

            await target.SendFailedSessionAsync(reason);
        }

        #endregion

        private class TestServerChannel : ServerChannel
        {
            public TestServerChannel(SessionState state, Guid sessionId, Node serverNode, ITransport transport, TimeSpan sendTimeout, Node remoteNode)
                : base(sessionId, serverNode, transport, sendTimeout)
            {                
                base.State = state;
                base.RemoteNode = remoteNode;
            }
        }

    }
}