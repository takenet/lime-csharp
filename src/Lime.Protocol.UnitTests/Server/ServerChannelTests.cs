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
                                        e.Authentication == null &&
                                        e.Mode == SessionMode.Node &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            Assert.AreEqual(target.State, SessionState.Negotiating);
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

            var schemeOptions = DataUtil.CreateSchemeOptions();

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
                                        e.Authentication == null &&
                                        e.Mode == SessionMode.Node &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            Assert.AreEqual(target.State, SessionState.Authenticating);
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

        [TestMethod]
        [TestCategory("SendAuthenticatingSessionAsync")]
        public async Task SendAuthenticatingSessionAsync_ValidRoundtrip_CallsTransport()
        {
            var target = GetTarget();

            // Sets the state to Authenticating
            var schemeOptions = DataUtil.CreateSchemeOptions();
            await target.SendAuthenticatingSessionAsync(schemeOptions);            

            var authenticationRoundtrip = DataUtil.CreatePlainAuthentication();

            await target.SendAuthenticatingSessionAsync(authenticationRoundtrip);

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Authenticating &&
                                        e.Authentication == authenticationRoundtrip &&
                                        e.SchemeOptions == null &&
                                        e.From.Equals(target.LocalNode) &&
                                        e.To == null &&
                                        e.CompressionOptions == null &&
                                        e.Compression == null &&
                                        e.EncryptionOptions == null &&
                                        e.Encryption == null &&
                                        e.Mode == SessionMode.Node &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [TestCategory("SendAuthenticatingSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendAuthenticatingSessionAsync_NullRoundtrip_ThrowsArgumentNullException()
        {
            var target = GetTarget();

            // Sets the state to Authenticating
            var schemeOptions = DataUtil.CreateSchemeOptions();
            await target.SendAuthenticatingSessionAsync(schemeOptions);

            Authentication authenticationRoundtrip = null;

            await target.SendAuthenticatingSessionAsync(authenticationRoundtrip);
        }

        [TestMethod]
        [TestCategory("SendAuthenticatingSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendAuthenticatingSessionAsync_ValidRoundtripNewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget();

            // Sets the state to Authenticating            
            var authenticationRoundtrip = DataUtil.CreatePlainAuthentication();

            await target.SendAuthenticatingSessionAsync(authenticationRoundtrip);
        }

        #endregion

        #region SendEstablishedSessionAsync

        [TestMethod]
        [TestCategory("SendEstablishedSessionAsync")]
        public async Task SendEstablishedSessionAsync_ValidArgumentsAuthenticatingState_CallsTransport()
        {
            var target = GetTarget();
            await target.SetStateAsync(SessionState.Authenticating);

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
            var target = GetTarget();
            await target.SetStateAsync(SessionState.Authenticating);


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

        #region SendFinishedSessionAsync

        [TestMethod]
        [TestCategory("SendFinishedSessionAsync")]
        public async Task SendFinishedSessionAsync_EstablishedState_CallsAndClosesTransport()
        {
            var target = GetTarget();                       
            await target.SetStateAsync(SessionState.Established);

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

        #region OnSessionReceivedAsync

        [TestMethod]
        [TestCategory("OnSessionReceivedAsync")]
        public void OnSessionReceivedAsync_NewSession_RaisesNewSessionReceived()
        {
            var target = GetTarget();
            bool newSessionReceivedRaised = false;

            var session = DataUtil.CreateSession();
            session.State = SessionState.New;

            target.NewSessionReceived += (sender, e) => newSessionReceivedRaised = !newSessionReceivedRaised && e.Envelope == session;

            _transport.ReceiveEnvelope(session);

            Assert.IsTrue(newSessionReceivedRaised);
        }

        [TestMethod]
        [TestCategory("OnSessionReceivedAsync")]
        public void OnSessionReceivedAsync_FinishingSession_RaisesFinishingSessionReceived()
        {
            var target = GetTarget();
            bool finishingSessionReceivedRaised = false;

            var session = DataUtil.CreateSession();
            session.State = SessionState.Finishing;

            target.FinishingSessionReceived += (sender, e) => finishingSessionReceivedRaised = !finishingSessionReceivedRaised && e.Envelope == session;

            _transport.ReceiveEnvelope(session);

            Assert.IsTrue(finishingSessionReceivedRaised);
        } 

        #endregion

        #region OnMessageReceivedAsync

        [TestMethod]
        [TestCategory("OnMessageReceivedAsync")]
        public async Task OnMessageReceivedAsync_EstablishedState_RaisesMessageReceived()
        {
            var target = GetTarget();
            await target.SetStateAsync(SessionState.Established);

            bool messageReceivedRaised = false;

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            target.MessageReceived += (sender, e) => messageReceivedRaised = !messageReceivedRaised && e.Envelope == message;
            _transport.ReceiveEnvelope(message);

            Assert.IsTrue(messageReceivedRaised);
        }

        [TestMethod]
        [TestCategory("OnMessageReceivedAsync")]
        public void OnMessageReceivedAsync_NewState_CallsSessionFailedAsyncAndClosesTransport()
        {
            var target = GetTarget();

            bool messageReceivedRaised = false;

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            target.MessageReceived += (sender, e) => messageReceivedRaised = !messageReceivedRaised && e.Envelope == message;
            _transport.ReceiveEnvelope(message);

            Assert.IsFalse(messageReceivedRaised);

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Failed &&
                                        e.Reason != null &&
                                        e.Reason.Code == ReasonCodes.SESSION_INVALID_ACTION_FOR_STATE &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            _transport.Verify(
                t => t.CloseAsync(
                    It.IsAny<CancellationToken>()));

        }

        #endregion

        #region OnCommandReceivedAsync

        [TestMethod]
        [TestCategory("OnCommandReceivedAsync")]
        public async Task OnCommandReceivedAsync_EstablishedState_RaisesCommandReceived()
        {
            var target = GetTarget();
            await target.SetStateAsync(SessionState.Established);

            bool commandReceivedRaised = false;

            var content = DataUtil.CreateTextContent();
            var command = DataUtil.CreateCommand(content);

            target.CommandReceived += (sender, e) => commandReceivedRaised = !commandReceivedRaised && e.Envelope == command;
            _transport.ReceiveEnvelope(command);

            Assert.IsTrue(commandReceivedRaised);
        }

        [TestMethod]
        [TestCategory("OnCommandReceivedAsync")]
        public void OnCommandReceivedAsync_NewState_CallsSessionFailedAsyncAndClosesTransport()
        {
            var target = GetTarget();

            bool commandReceivedRaised = false;

            var content = DataUtil.CreateTextContent();
            var command = DataUtil.CreateCommand(content);

            target.CommandReceived += (sender, e) => commandReceivedRaised = !commandReceivedRaised && e.Envelope == command;
            _transport.ReceiveEnvelope(command);

            Assert.IsFalse(commandReceivedRaised);

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Failed &&
                                        e.Reason != null &&
                                        e.Reason.Code == ReasonCodes.SESSION_INVALID_ACTION_FOR_STATE &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            _transport.Verify(
                t => t.CloseAsync(
                    It.IsAny<CancellationToken>()));

        }

        #endregion


        #region OnNotificationReceivedAsync

        [TestMethod]
        [TestCategory("OnNotificationReceivedAsync")]
        public async Task OnNotificationReceivedAsync_EstablishedState_RaisesNotificationReceived()
        {
            var target = GetTarget();
            await target.SetStateAsync(SessionState.Established);

            bool notificationReceivedRaised = false;

            var notification = DataUtil.CreateNotification(Event.Received);

            target.NotificationReceived += (sender, e) => notificationReceivedRaised = !notificationReceivedRaised && e.Envelope == notification;
            _transport.ReceiveEnvelope(notification);

            Assert.IsTrue(notificationReceivedRaised);
        }

        [TestMethod]
        [TestCategory("OnNotificationReceivedAsync")]
        public void OnNotificationReceivedAsync_NewState_CallsSessionFailedAsyncAndClosesTransport()
        {
            var target = GetTarget();

            bool notificationReceivedRaised = false;

            var notification = DataUtil.CreateNotification(Event.Received);

            target.NotificationReceived += (sender, e) => notificationReceivedRaised = !notificationReceivedRaised && e.Envelope == notification;
            _transport.ReceiveEnvelope(notification);

            Assert.IsFalse(notificationReceivedRaised);

            _transport.Verify(
                t => t.SendAsync(
                    It.Is<Session>(e => e.State == SessionState.Failed &&
                                        e.Reason != null &&
                                        e.Reason.Code == ReasonCodes.SESSION_INVALID_ACTION_FOR_STATE &&
                                        e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            _transport.Verify(
                t => t.CloseAsync(
                    It.IsAny<CancellationToken>()));

        }

        #endregion
    }
}