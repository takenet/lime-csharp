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
        #region Private fields

        private Mock<ITransport> _transport;
        private TimeSpan _sendTimeout;
        private Guid _sessionId;
        private Node _serverNode;

        #endregion

        #region Constructor

        public ServerChannelTests()
        {
            _transport = new Mock<ITransport>();
            _sendTimeout = TimeSpan.FromSeconds(30);
            _sessionId = Guid.NewGuid();
            _serverNode = DataUtil.CreateNode();
        }

        #endregion

        private ServerChannel GetTarget(SessionState state = SessionState.New, Node remoteNode = null)
        {
            return new TestServerChannel(
                state,
                _sessionId,
                _serverNode,
                _transport.Object,
                _sendTimeout,
                remoteNode);
        }

        #region ReceiveNewSessionAsync

        [TestMethod]
        [TestCategory("ReceiveNewSessionAsync")]
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

        [TestMethod]
        [TestCategory("ReceiveNewSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveNewSessionAsync_NotNewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.Established);

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.ReceiveNewSessionAsync(cancellationToken);
        }

        [TestMethod]
        [TestCategory("ReceiveNewSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveNewSessionAsync_NewStateHasSessionPromise_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => tcs.Task)
                .Verifiable();

            var receiveSessionTask = ((ISessionChannel)target).ReceiveSessionAsync(
                cancellationToken);

            var actual = await target.ReceiveNewSessionAsync(cancellationToken);
        }

        #endregion

        #region NegotiateSessionAsync

        [TestMethod]
        [TestCategory("NegotiateSessionAsync")]
        public async Task NegotiateSessionAsync_NewStateValidOptions_CallsTransportAndReadsFromBuffer()
        {
            var target = GetTarget();           

            var compressionOptions = new SessionCompression[] { SessionCompression.None };
            var encryptionOptions = new SessionEncryption[] { SessionEncryption.None, SessionEncryption.TLS };
            var session = DataUtil.CreateSession(SessionState.Negotiating);

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actual = await target.NegotiateSessionAsync(
                cancellationToken, 
                compressionOptions, 
                encryptionOptions);

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
                                        e.Mode == SessionMode.Node &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            Assert.AreEqual(target.State, SessionState.Negotiating);
            Assert.AreEqual(session, actual);
        }

        [TestMethod]
        [TestCategory("NegotiateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task NegotiateSessionAsync_InvalidStateValidOptions_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.Negotiating);

            var compressionOptions = new SessionCompression[] { SessionCompression.None };
            var encryptionOptions = new SessionEncryption[] { SessionEncryption.None, SessionEncryption.TLS };

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.NegotiateSessionAsync(
                cancellationToken,
                compressionOptions,
                encryptionOptions);
        }

        [TestMethod]
        [TestCategory("NegotiateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task NegotiateSessionAsync_NewStateHasSessionPromise_ThrowsInvalidOperationException()
        {
            var target = GetTarget();

            var compressionOptions = new SessionCompression[] { SessionCompression.None };
            var encryptionOptions = new SessionEncryption[] { SessionEncryption.None, SessionEncryption.TLS };

            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => tcs.Task)
                .Verifiable();

            var receiveSessionTask = ((ISessionChannel)target).ReceiveSessionAsync(
                cancellationToken);

            var actual = await target.NegotiateSessionAsync(
                cancellationToken,
                compressionOptions,
                encryptionOptions);

        }

        [TestMethod]
        [TestCategory("NegotiateSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task NegotiateSessionAsync_NullCompressionOptions_ThrowsArgumentNullException()
        {
            var target = GetTarget();

            SessionCompression[] compressionOptions = null;
            var encryptionOptions = new SessionEncryption[] { SessionEncryption.None, SessionEncryption.TLS };

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.NegotiateSessionAsync(
                cancellationToken,
                compressionOptions,
                encryptionOptions);
        }

        [TestMethod]
        [TestCategory("NegotiateSessionAsync")]
        [ExpectedException(typeof(ArgumentException))]
        public async Task NegotiateSessionAsync_EmptyCompressionOptions_ThrowsArgumentNullException()
        {
            var target = GetTarget();

            var compressionOptions = new SessionCompression[0];
            var encryptionOptions = new SessionEncryption[] { SessionEncryption.None, SessionEncryption.TLS };

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.NegotiateSessionAsync(
                cancellationToken,
                compressionOptions,
                encryptionOptions);
        }

        [TestMethod]
        [TestCategory("NegotiateSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task NegotiateSessionAsync_NullEncryptionOptions_ThrowsArgumentNullException()
        {
            var target = GetTarget();

            var compressionOptions = new SessionCompression[] { SessionCompression.None };
            SessionEncryption[] encryptionOptions = null;

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.NegotiateSessionAsync(
                cancellationToken,
                compressionOptions,
                encryptionOptions);
        }

        [TestMethod]
        [TestCategory("NegotiateSessionAsync")]
        [ExpectedException(typeof(ArgumentException))]
        public async Task NegotiateSessionAsync_EmptyEncryptionOptions_ThrowsArgumentException()
        {
            var target = GetTarget();

            var compressionOptions = new SessionCompression[] { SessionCompression.None };
            var encryptionOptions = new SessionEncryption[0];

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.NegotiateSessionAsync(
                cancellationToken,
                compressionOptions,
                encryptionOptions);
        }

        #endregion

        #region SendNegotiatingSessionAsync

        [TestMethod]
        [TestCategory("SendNegotiatingSessionAsync")]
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
                                        e.Mode == SessionMode.Node &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        #endregion

        #region AuthenticateSessionAsync

        [TestMethod]
        [TestCategory("AuthenticateSessionAsync")]
        public async Task AuthenticateSessionAsync_NegotiatingStateValidOptions_CallsTransportAndReadsFromBuffer()
        {
            var target = GetTarget(SessionState.Negotiating);

            var schemeOptions = DataUtil.CreateSchemeOptions();

            var session = DataUtil.CreateSession(SessionState.Authenticating);

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actual = await target.AuthenticateSessionAsync(
                cancellationToken,
                schemeOptions);

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
                                        e.Mode == SessionMode.Node &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            Assert.AreEqual(target.State, SessionState.Authenticating);
            Assert.AreEqual(session, actual);
        }

        [TestMethod]
        [TestCategory("AuthenticateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task AuthenticateSessionAsync_NegotiatingStateValidOptionsHasPromises_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.Negotiating);

            var schemeOptions = DataUtil.CreateSchemeOptions();

            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => tcs.Task)
                .Verifiable();

            var receiveSessionTask = ((ISessionChannel)target).ReceiveSessionAsync(
                cancellationToken);

            var actual = await target.AuthenticateSessionAsync(
                cancellationToken,
                schemeOptions);
        }

        [TestMethod]
        [TestCategory("AuthenticateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task AuthenticateSessionAsync_InvalidStateValidOptions_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.Established);

            var schemeOptions = DataUtil.CreateSchemeOptions();            

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.AuthenticateSessionAsync(
                cancellationToken,
                schemeOptions);
        }

        [TestMethod]
        [TestCategory("AuthenticateSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AuthenticateSessionAsync_NullOptions_ThrowsArgumentNullException()
        {
            var target = GetTarget(SessionState.Negotiating);

            AuthenticationScheme[] schemeOptions = null;

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.AuthenticateSessionAsync(
                cancellationToken,
                schemeOptions);
        }

        [TestMethod]
        [TestCategory("AuthenticateSessionAsync")]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AuthenticateSessionAsync_EmptyOptions_ThrowsArgumentException()
        {
            var target = GetTarget(SessionState.Negotiating);

            AuthenticationScheme[] schemeOptions = new AuthenticationScheme[0];

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.AuthenticateSessionAsync(
                cancellationToken,
                schemeOptions);
        }

        [TestMethod]
        [TestCategory("AuthenticateSessionAsync")]
        public async Task AuthenticateSessionAsync_AuthenticatingStateValidRoundtrip_CallsTransportAndReadsFromBuffer()
        {
            var target = GetTarget(SessionState.Authenticating);

            var authenticationRoundtrip = DataUtil.CreatePlainAuthentication();
            var session = DataUtil.CreateSession(SessionState.Authenticating);

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actual = await target.AuthenticateSessionAsync(
                cancellationToken,
                authenticationRoundtrip);

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
                                        e.Mode == SessionMode.Node &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            Assert.AreEqual(target.State, SessionState.Authenticating);
            Assert.AreEqual(session, actual);
        }

        [TestMethod]
        [TestCategory("AuthenticateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task AuthenticateSessionAsync_AuthenticatingStateValidRoundtripHasPromises_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.Authenticating);

            var authenticationRoundtrip = DataUtil.CreatePlainAuthentication();
            var session = DataUtil.CreateSession(SessionState.Authenticating);

            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => tcs.Task)
                .Verifiable();

            var receiveSessionTask = ((ISessionChannel)target).ReceiveSessionAsync(
                cancellationToken);

            var actual = await target.AuthenticateSessionAsync(
                cancellationToken,
                authenticationRoundtrip);
        }

        [TestMethod]
        [TestCategory("AuthenticateSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AuthenticateSessionAsync_AuthenticatingStateNullRoundtrip_ThrowsArgumentNullException()
        {
            var target = GetTarget(SessionState.Authenticating);

            Authentication authenticationRoundtrip = null;

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.AuthenticateSessionAsync(
                cancellationToken,
                authenticationRoundtrip);
        }

        [TestMethod]
        [TestCategory("AuthenticateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task AuthenticateSessionAsync_InvalidStateValidRoundtrip_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var authenticationRoundtrip = DataUtil.CreatePlainAuthentication();

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.AuthenticateSessionAsync(
                cancellationToken,
                authenticationRoundtrip);
        }

        #endregion

        #region SendEstablishedSessionAsync

        [TestMethod]
        [TestCategory("SendEstablishedSessionAsync")]
        public async Task SendEstablishedSessionAsync_ValidArgumentsAuthenticatingState_CallsTransport()
        {
            var target = GetTarget(SessionState.Authenticating);

            var node = DataUtil.CreateNode();
            var mode = SessionMode.Server;

            await target.SendEstablishedSessionAsync(node, mode);

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
                                        e.Mode == mode &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            Assert.AreEqual(target.State, SessionState.Established);
            Assert.AreEqual(target.RemoteNode, node);
            Assert.AreEqual(target.Mode, mode);
        }

        [TestMethod]
        [TestCategory("SendEstablishedSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendEstablishedSessionAsync_NullNodeAuthenticatingState_ThrowsArgumentNullException()
        {
            var target = GetTarget(SessionState.Authenticating);

            Node node = null;
            var mode = SessionMode.Node;

            await target.SendEstablishedSessionAsync(node, mode);
        }

        [TestMethod]
        [TestCategory("SendEstablishedSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendEstablishedSessionAsync_ValidArgumentsNewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget();

            var node = DataUtil.CreateNode();
            var mode = SessionMode.Node;

            await target.SendEstablishedSessionAsync(node, mode);
        }

        #endregion

        #region ReceiveFinishingSessionAsync

        [TestMethod]
        [TestCategory("ReceiveFinishingSessionAsync")]
        public async Task ReceiveFinishingSessionAsync_EstablishedState_ReadsTransport()
        {
            var target = GetTarget(SessionState.Established);

            var session = DataUtil.CreateSession(SessionState.Finishing);
            session.Id = target.SessionId;

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actual = await target.ReceiveFinishingSessionAsync(cancellationToken);

            Assert.AreEqual(session, actual);
            _transport.Verify();
        }

        [TestMethod]
        [TestCategory("ReceiveFinishingSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveFinishingSessionAsync_InvalidState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.Authenticating);

            var cancellationToken = DataUtil.CreateCancellationToken();

            var actual = await target.ReceiveFinishingSessionAsync(cancellationToken);
        }

        [TestMethod]
        [TestCategory("ReceiveFinishingSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveFinishingSessionAsync_EstablishedStateHasPromises_ReadsTransport()
        {
            var target = GetTarget(SessionState.Established);

            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => tcs.Task)
                .Verifiable();

            var receiveSessionTask = ((ISessionChannel)target).ReceiveSessionAsync(
                cancellationToken);

            var actual = await target.ReceiveFinishingSessionAsync(cancellationToken);
        }

        #endregion

        #region SendFinishedSessionAsync

        [TestMethod]
        [TestCategory("SendFinishedSessionAsync")]
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
                                        e.Encryption == null &&
                                        e.Mode == target.Mode),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            _transport.Verify(
                t => t.CloseAsync(
                    It.IsAny<CancellationToken>()));

            Assert.AreEqual(target.State, SessionState.Finished);
        }

        [TestMethod]
        [TestCategory("SendFinishedSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendFinishedSessionAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget();
            await target.SendFinishedSessionAsync();
        }

        #endregion

        #region SendFailedSessionAsync

        [TestMethod]
        [TestCategory("SendFailedSessionAsync")]
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
                                        e.Encryption == null &&
                                        e.Mode == target.Mode),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            _transport.Verify(
                t => t.CloseAsync(
                    It.IsAny<CancellationToken>()));

            Assert.AreEqual(target.State, SessionState.Failed);
        }

        [TestMethod]
        [TestCategory("SendFailedSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendFailedSessionAsync_NullReason_ThrowsArgumentNullException()
        {
            var target = GetTarget();
            Reason reason = null;

            await target.SendFailedSessionAsync(reason);
        }

        #endregion

        #region OnSessionReceivedAsync

        [TestMethod]
        [TestCategory("OnSessionReceivedAsync")]
        public async Task OnSessionReceivedAsync_AnyState_SendsToBuffer()
        {
            var target = GetTarget() as TestServerChannel;

            var session = DataUtil.CreateSession();
            var cancellationToken = DataUtil.CreateCancellationToken();

            await target.CallsOnSessionReceivedAsync(session);

            var actual = await ((ISessionChannel)target).ReceiveSessionAsync(cancellationToken);

            Assert.AreEqual(session, actual);
        }

        #endregion

        #region OnMessageReceivedAsync

        [TestMethod]
        [TestCategory("OnMessageReceivedAsync")]
        public async Task OnMessageReceivedAsync_EstablishedState_SendsToBuffer()
        {
            var target = GetTarget(state: SessionState.Established) as TestServerChannel;

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            var cancellationToken = DataUtil.CreateCancellationToken();

            await target.CallsOnMessageReceivedAsync(message);

            var actual = await target.ReceiveMessageAsync(cancellationToken);

            Assert.AreEqual(message, actual);
        }

        [TestMethod]
        [TestCategory("OnMessageReceivedAsync")]
        public async Task OnMessageReceivedAsync_InvalidState_SendsFailedSession()
        {
            var target = GetTarget(state: SessionState.Authenticating) as TestServerChannel;

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            var cancellationToken = DataUtil.CreateCancellationToken();

            await target.CallsOnMessageReceivedAsync(message);

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Failed &&
                                        e.Reason != null &&
                                        e.Reason.Code == ReasonCodes.SESSION_INVALID_ACTION_FOR_STATE &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        #endregion

        #region OnCommandReceivedAsync

        [TestMethod]
        [TestCategory("OnCommandReceivedAsync")]
        public async Task OnCommandReceivedAsync_EstablishedState_SendsToBuffer()
        {
            var target = GetTarget(state: SessionState.Established) as TestServerChannel;

            var resource = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(resource);
            var cancellationToken = DataUtil.CreateCancellationToken();

            await target.CallsOnCommandReceivedAsync(command);

            var actual = await target.ReceiveCommandAsync(cancellationToken);

            Assert.AreEqual(command, actual);
        }

        [TestMethod]
        [TestCategory("OnCommandReceivedAsync")]
        public async Task OnCommandReceivedAsync_InvalidState_SendsFailedSession()
        {
            var target = GetTarget(state: SessionState.Authenticating) as TestServerChannel;

            var resource = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(resource);
            var cancellationToken = DataUtil.CreateCancellationToken();

            await target.CallsOnCommandReceivedAsync(command);

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Failed &&
                                        e.Reason != null &&
                                        e.Reason.Code == ReasonCodes.SESSION_INVALID_ACTION_FOR_STATE &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        #endregion 

        #region OnNotificationReceivedAsync

        [TestMethod]
        [TestCategory("OnNotificationReceivedAsync")]
        public async Task OnNotificationReceivedAsync_EstablishedState_SendsToBuffer()
        {
            var target = GetTarget(state: SessionState.Established) as TestServerChannel;

            var notification = DataUtil.CreateNotification(Event.Received);
            var cancellationToken = DataUtil.CreateCancellationToken();

            await target.CallsOnNotificationReceivedAsync(notification);

            var actual = await target.ReceiveNotificationAsync(cancellationToken);

            Assert.AreEqual(notification, actual);
        }

        [TestMethod]
        [TestCategory("OnNotificationReceivedAsync")]
        public async Task OnNotificationReceivedAsync_InvalidState_SendsFailedSession()
        {
            var target = GetTarget(state: SessionState.Authenticating) as TestServerChannel;

            var notification = DataUtil.CreateNotification(Event.Received);
            var cancellationToken = DataUtil.CreateCancellationToken();

            await target.CallsOnNotificationReceivedAsync(notification);

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Failed &&
                                        e.Reason != null &&
                                        e.Reason.Code == ReasonCodes.SESSION_INVALID_ACTION_FOR_STATE &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
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

            public Task CallsOnMessageReceivedAsync(Message message)
            {
                return base.OnMessageReceivedAsync(message);
            }

            public Task CallsOnCommandReceivedAsync(Command command)
            {
                return base.OnCommandReceivedAsync(command);
            }

            public Task CallsOnNotificationReceivedAsync(Notification notification)
            {
                return base.OnNotificationReceivedAsync(notification);
            }

            public Task CallsOnSessionReceivedAsync(Session session)
            {
                return base.OnSessionReceivedAsync(session);
            }

        }

    }
}