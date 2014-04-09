using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Moq;
using System.Threading;
using Lime.Protocol.Security;
using Lime.Protocol.Resources;

namespace Lime.Protocol.UnitTests.Client
{
    [TestClass]
    public class ClientChannelTests
    {
        private Mock<ITransport> _transport;
        private TimeSpan _sendTimeout;

        #region Constructor

        public ClientChannelTests()
        {
            _transport = new Mock<ITransport>();
            _sendTimeout = TimeSpan.FromSeconds(30);
        }

        #endregion

        public ClientChannel GetTarget(Guid? sessionId = null, SessionState state = SessionState.New, bool autoReplyPings = true, bool autoNotifyReceipt = true)
        {
            return new TestClientChannel(
                sessionId,
                state,
                _transport.Object,
                _sendTimeout,
                autoReplyPings,
                autoNotifyReceipt
                );
        }

        #region StartNewSessionAsync

        [TestMethod]
        [TestCategory("StartNewSessionAsync")]
        public async Task StartNewSessionAsync_NewState_CallsTransportAndReadsFromBuffer()
        {
            var target = GetTarget();            
            var cancellationToken = DataUtil.CreateCancellationToken();

            var session = DataUtil.CreateSession(SessionState.Authenticating);

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actualSession = await target.StartNewSessionAsync(cancellationToken);

            _transport.Verify(
                t => t.SendAsync(It.Is<Session>(e => e.State == SessionState.New),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            Assert.AreEqual(session, actualSession);
        }

        [TestMethod]
        [TestCategory("SendNewSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task StartNewSessionAsync_InvalidState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(state: SessionState.Established);

            var cancellationToken = DataUtil.CreateCancellationToken();
            var actualSession = await target.StartNewSessionAsync(cancellationToken);
        }

        [TestMethod]
        [TestCategory("SendNewSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task StartNewSessionAsync_NewStateHasSessionPromise_ThrowsInvalidOperationException()
        {
            var target = GetTarget(state: SessionState.New);

            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => tcs.Task);

            var startSessionTask = target.StartNewSessionAsync(cancellationToken);

            var actualSession = await target.StartNewSessionAsync(cancellationToken);
        }

        #endregion

        #region NegotiateSessionAsync

        [TestMethod]
        [TestCategory("NegotiateSessionAsync")]
        public async Task NegotiateSessionAsync_NegotiatingState_CallsTransportAndReadsFromBuffer()
        {
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Negotiating);

            var cancellationToken = DataUtil.CreateCancellationToken();
            var compression = SessionCompression.GZip;
            var encryption = SessionEncryption.TLS;

            var session = DataUtil.CreateSession(SessionState.Negotiating);
            session.Id = target.SessionId;

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actualSession = await target.NegotiateSessionAsync(cancellationToken, compression, encryption);

            _transport.Verify(
                t => t.SendAsync(It.Is<Session>(
                        e => e.State == SessionState.Negotiating &&
                             e.Id == target.SessionId &&
                             e.Compression == compression &&
                             e.Encryption == encryption),
                    It.IsAny<CancellationToken>()),
                    Times.Once());

            Assert.AreEqual(session, actualSession);
        }

        [TestMethod]
        [TestCategory("NegotiateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task NegotiateSessionAsync_InvalidState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(state: SessionState.New);

            var cancellationToken = DataUtil.CreateCancellationToken();
            var compression = SessionCompression.GZip;
            var encryption = SessionEncryption.TLS;

            var actualSession = await target.NegotiateSessionAsync(cancellationToken, compression, encryption);
        }

        [TestMethod]
        [TestCategory("NegotiateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task NegotiateSessionAsync_NewStateHasSessionPromise_ThrowsInvalidOperationException()
        {
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Negotiating);

            var cancellationToken = DataUtil.CreateCancellationToken();
            var compression = SessionCompression.GZip;
            var encryption = SessionEncryption.TLS;

            var session = DataUtil.CreateSession(SessionState.Negotiating);
            session.Id = target.SessionId;

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => tcs.Task)
                .Verifiable();

            var negotiateSessionTask = target.NegotiateSessionAsync(cancellationToken, compression, encryption);

            var actualSession = await target.NegotiateSessionAsync(cancellationToken, compression, encryption);
        }
  
        #endregion

        #region AuthenticateSessionAsync

        [TestMethod]
        [TestCategory("AuthenticateSessionAsync")]
        public async Task AuthenticateSessionAsync_AuthenticatingState_CallsTransportAndReadsFromBuffer()
        {
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Authenticating);

            var cancellationToken = DataUtil.CreateCancellationToken();
            var localIdentity = DataUtil.CreateIdentity();
            var localInstance = DataUtil.CreateInstanceName();
            var authentication = DataUtil.CreateAuthentication(Security.AuthenticationScheme.Plain);
            var sessionMode = SessionMode.Node;

            var session = DataUtil.CreateSession(SessionState.Established);
            session.Id = target.SessionId;

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actualSession = await target.AuthenticateSessionAsync(
                cancellationToken,
                localIdentity,
                authentication,
                localInstance,
                sessionMode);

                _transport.Verify(
                    t => t.SendAsync(It.Is<Session>(
                            e => e.State == SessionState.Authenticating &&
                                 e.Id == target.SessionId &&
                                 e.From.ToIdentity().Equals(localIdentity) &&
                                 e.From.Instance.Equals(localInstance) &&
                                 e.Mode == sessionMode &&
                                 e.Authentication == authentication),
                        It.IsAny<CancellationToken>()),
                        Times.Once());            

            Assert.AreEqual(session, actualSession);
        }

        [TestMethod]
        [TestCategory("AuthenticateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task AuthenticateSessionAsync_InvalidState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Established);

            var cancellationToken = DataUtil.CreateCancellationToken();
            var localIdentity = DataUtil.CreateIdentity();
            var localInstance = DataUtil.CreateInstanceName();
            var authentication = DataUtil.CreateAuthentication(Security.AuthenticationScheme.Plain);
            var sessionMode = SessionMode.Node;

            var actualSession = await target.AuthenticateSessionAsync(
                cancellationToken,
                localIdentity,
                authentication,
                localInstance,
                sessionMode);
        }

        [TestMethod]
        [TestCategory("AuthenticateSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AuthenticateSessionAsync_NullIdentity_ThrowsArgumentNullException()
        {
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Authenticating);

            var cancellationToken = DataUtil.CreateCancellationToken();
            Identity localIdentity = null;
            var localInstance = DataUtil.CreateInstanceName();
            var authentication = DataUtil.CreateAuthentication(Security.AuthenticationScheme.Plain);
            var sessionMode = SessionMode.Node;

            var actualSession = await target.AuthenticateSessionAsync(
                cancellationToken,
                localIdentity,
                authentication,
                localInstance,
                sessionMode);
        }

        [TestMethod]
        [TestCategory("AuthenticateSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AuthenticateSessionAsync_NullAuthentication_ThrowsArgumentNullException()
        {
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Authenticating);

            var cancellationToken = DataUtil.CreateCancellationToken();
            var localIdentity = DataUtil.CreateIdentity();
            var localInstance = DataUtil.CreateInstanceName();
            Authentication authentication = null;
            var sessionMode = SessionMode.Node;

            var actualSession = await target.AuthenticateSessionAsync(
                cancellationToken,
                localIdentity,
                authentication,
                localInstance,
                sessionMode);
        }

        [TestMethod]
        [TestCategory("AuthenticateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task AuthenticateSessionAsync_AuthenticatingStateHasSessionPromise_ThrowsInvalidOperationException()
        {
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Authenticating);

            var cancellationToken = DataUtil.CreateCancellationToken();
            var localIdentity = DataUtil.CreateIdentity();
            var localInstance = DataUtil.CreateInstanceName();
            var authentication = DataUtil.CreateAuthentication(Security.AuthenticationScheme.Plain);
            var sessionMode = SessionMode.Node;

            var session = DataUtil.CreateSession(SessionState.Authenticating);
            session.Id = target.SessionId;

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => tcs.Task)
                .Verifiable();

            var authenticateSessionTask = target.AuthenticateSessionAsync(
                cancellationToken,
                localIdentity,
                authentication,
                localInstance,
                sessionMode);

            var actualSession = await target.AuthenticateSessionAsync(
                cancellationToken,
                localIdentity,
                authentication,
                localInstance,
                sessionMode);
        }

        #endregion

        #region SendReceivedNotificationAsync

        [TestMethod]
        [TestCategory("SendReceivedNotificationAsync")]
        public async Task SendReceivedNotificationAsync_EstablishedState_CallsTransport()
        {
            var target = GetTarget(state: SessionState.Established);

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            await target.SendReceivedNotificationAsync(message.Id.Value, message.From);

            _transport.Verify(
                t => t.SendAsync(It.Is<Notification>(
                        n => n.Id == message.Id &&
                             n.To.Equals(message.From) &&
                             n.Event == Event.Received),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [TestCategory("SendReceivedNotificationAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendReceivedNotificationAsync_NullTo_ThrowsArgumentNullException()
        {
            var target = GetTarget(state: SessionState.Established);

            var message = DataUtil.CreateMessage(content: null);

            await target.SendReceivedNotificationAsync(message.Id.Value, null);
        }

        #endregion

        #region SendFinishingSessionAsync

        [TestMethod]
        [TestCategory("SendFinishingSessionAsync")]
        public async Task SendFinishingSessionAsync_EstablishedState_CallsTransport()
        {
            var target = GetTarget(state: SessionState.Established);

            await target.SendFinishingSessionAsync();

            _transport.Verify(
                t => t.SendAsync(It.Is<Session>(
                        e => e.State == SessionState.Finishing &&
                             e.Id == target.SessionId),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [TestCategory("SendFinishingSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendFinishingSessionAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(state: SessionState.New); 
            await target.SendFinishingSessionAsync();
        }

        #endregion

        #region ReceiveSessionFinishedAsync

        [TestMethod]
        [TestCategory("ReceiveSessionFinishedAsync")]
        public async Task ReceiveSessionAsync_EstablishedState_ReadsTransport()
        {
            var target = GetTarget(state: SessionState.Established);

            var session = DataUtil.CreateSession(SessionState.Finished);
            session.Id = target.SessionId;

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actual = await target.ReceiveSessionFinishedAsync(cancellationToken);

            Assert.AreEqual(session, actual);
            _transport.Verify();
        }

        [TestMethod]
        [TestCategory("ReceiveSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveSessionAsync_InvalidState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(state: SessionState.Finished);

            var session = DataUtil.CreateSession();

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session));

            var actual = await target.ReceiveSessionFinishedAsync(cancellationToken);
        }

        [TestMethod]
        [TestCategory("ReceiveSessionFinishedAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveSessionAsync_EstablishedStateHasSessionPromise_ThrowsInvalidOperationException()
        {
            var target = GetTarget(state: SessionState.Established);


            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => tcs.Task)
                .Verifiable();

            var receiveSessionFinishedTask = target.ReceiveSessionFinishedAsync(cancellationToken);

            var actual = await target.ReceiveSessionFinishedAsync(cancellationToken);

        }

        #endregion

        #region OnMessageReceivedAsync

        [TestMethod]
        [TestCategory("OnMessageReceivedAsync")]
        public async Task OnMessageReceivedAsync_MessageReceivedAndAutoNotifyReceiptTrue_SendsNotificationToTransport()
        {
            var target = GetTarget(state: SessionState.Established, autoNotifyReceipt: true);

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(message))
                .Verifiable();

            var actual = await target.ReceiveMessageAsync(cancellationToken);

            _transport.Verify(
                t => t.SendAsync(It.Is<Notification>(
                        n => n.Id == message.Id &&
                             n.To.Equals(message.From) &&
                             n.Event == Event.Received),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [TestCategory("OnMessageReceivedAsync")]
        public async Task OnMessageReceivedAsync_MessageReceivedAndAutoNotifyReceiptFalse_DoNotSendsNotificationToTransport()
        {
            var target = GetTarget(state: SessionState.Established, autoNotifyReceipt: false);

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(message))
                .Verifiable();

            var actual = await target.ReceiveMessageAsync(cancellationToken);

            _transport.Verify(
                t => t.SendAsync(It.Is<Notification>(
                        n => n.Id == message.Id &&
                             n.To.Equals(message.From) &&
                             n.Event == Event.Received),
                    It.IsAny<CancellationToken>()),
                    Times.Never());
        }

        [TestMethod]
        [TestCategory("OnMessageReceivedAsync")]
        public async Task OnMessageReceivedAsync_FireAndForgetMessageReceivedAndAutoNotifyReceiptTrue_DoNotSendsNotificationToTransport()
        {
            var target = GetTarget(state: SessionState.Established, autoNotifyReceipt: true);

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            message.Id = null;

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(message))
                .Verifiable();

            var actual = await target.ReceiveMessageAsync(cancellationToken);

            _transport.Verify(
                t => t.SendAsync(It.Is<Notification>(
                        n => n.Id == message.Id &&
                             n.To.Equals(message.From) &&
                             n.Event == Event.Received),
                    It.IsAny<CancellationToken>()),
                    Times.Never());
        }

        #endregion

        #region OnCommandReceivedAsync

        [TestMethod]
        [TestCategory("OnCommandReceivedAsync")]
        public async Task OnCommandReceivedAsync_PingCommandReceivedAndAutoReplyPingsTrue_SendsPingCommandToTransport()
        {
            var target = GetTarget(state: SessionState.Established, autoReplyPings: true);

            var ping = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(ping);
            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(command))
                .Verifiable();

            var actual = await target.ReceiveCommandAsync(cancellationToken);

            _transport.Verify(
                t => t.SendAsync(It.Is<Command>(
                        c => c.Id == command.Id &&
                             c.To.Equals(command.From) &&
                             c.Resource is Ping &&
                             c.Status == CommandStatus.Success),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [TestCategory("OnCommandReceivedAsync")]
        public async Task OnCommandReceivedAsync_PingCommandReceivedAndAutoReplyPingsFalse_DoNotSendsPingCommandToTransport()
        {
            var target = GetTarget(state: SessionState.Established, autoReplyPings: false);

            var ping = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(ping);
            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(command))
                .Verifiable();

            var actual = await target.ReceiveCommandAsync(cancellationToken);

            _transport.Verify(
                t => t.SendAsync(It.Is<Command>(
                        c => c.Id == command.Id &&
                             c.To.Equals(command.From) &&
                             c.Resource is Ping &&
                             c.Status == CommandStatus.Success),
                    It.IsAny<CancellationToken>()),
                    Times.Never());
        }

        [TestMethod]
        [TestCategory("OnCommandReceivedAsync")]
        public async Task OnCommandReceivedAsync_PingResponseCommandReceivedAndAutoReplyPingsTrue_DoNotSendsPingCommandToTransport()
        {
            var target = GetTarget(state: SessionState.Established, autoReplyPings: true);

            var ping = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(ping, status: CommandStatus.Success);
            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(command))
                .Verifiable();

            var actual = await target.ReceiveCommandAsync(cancellationToken);

            _transport.Verify(
                t => t.SendAsync(It.Is<Command>(
                        c => c.Id == command.Id &&
                             c.To.Equals(command.From) &&
                             c.Resource is Ping &&
                             c.Status == CommandStatus.Success),
                    It.IsAny<CancellationToken>()),
                    Times.Never());
        }

        #endregion

        #region OnSessionReceivedAsync

        [TestMethod]
        [TestCategory("OnSessionReceivedAsync")]
        public async Task OnSessionReceivedAsync_AuthenticatingStateEstablishedSessionReceived_SetsStateAndNodeProperties()
        {
            var target = GetTarget(state: SessionState.Authenticating, autoNotifyReceipt: true);

            var authentication = DataUtil.CreateAuthentication(AuthenticationScheme.Plain);
            var identity = DataUtil.CreateIdentity();
            var session = DataUtil.CreateSession(SessionState.Established);            
            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actual = await target.AuthenticateSessionAsync(
                cancellationToken,
                identity,
                authentication);

            Assert.IsTrue(target.State == session.State);
            Assert.IsTrue(target.LocalNode.Equals(session.To));
            Assert.IsTrue(target.RemoteNode.Equals(session.From));
        }

        [TestMethod]
        [TestCategory("OnSessionReceivedAsync")]
        public async Task OnSessionReceivedAsync_EstablishedStateFinishedSessionReceived_SetsStateAndClosesTransport()
        {
            var target = GetTarget(state: SessionState.Established, autoNotifyReceipt: true);
            
            
            var session = DataUtil.CreateSession(SessionState.Finished);
            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actual = await target.ReceiveSessionFinishedAsync(
                cancellationToken);

            Assert.IsTrue(target.State == session.State);

            _transport
                .Verify(t =>
                    t.CloseAsync(It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [TestCategory("OnSessionReceivedAsync")]
        public async Task OnSessionReceivedAsync_EstablishedStateFailedSessionReceived_SetsStateAndClosesTransport()
        {
            var target = GetTarget(state: SessionState.Established, autoNotifyReceipt: true);


            var session = DataUtil.CreateSession(SessionState.Failed);
            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actual = await target.ReceiveSessionFinishedAsync(
                cancellationToken);

            Assert.IsTrue(target.State == session.State);

            _transport
                .Verify(t =>
                    t.CloseAsync(It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [TestCategory("OnSessionReceivedAsync")]
        public async Task OnSessionReceivedAsync_AuthenticatingStateFailedSessionReceived_SetsStateAndClosesTransport()
        {
            var target = GetTarget(state: SessionState.Authenticating, autoNotifyReceipt: true);

            var authentication = DataUtil.CreateAuthentication(AuthenticationScheme.Plain);
            var identity = DataUtil.CreateIdentity();
            var session = DataUtil.CreateSession(SessionState.Failed);
            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actual = await target.AuthenticateSessionAsync(
                cancellationToken,
                identity,
                authentication);

            Assert.IsTrue(target.State == session.State);

            _transport
                .Verify(t =>
                    t.CloseAsync(It.IsAny<CancellationToken>()),
                    Times.Once());
        }
    
        #endregion

        private class TestClientChannel : ClientChannel
        {
            public TestClientChannel(Guid? sessionId, SessionState state, ITransport transport, TimeSpan sendTimeout, bool autoReplyPings = true, bool autoNotifyReceipt = false)
                : base(transport, sendTimeout, autoReplyPings, autoNotifyReceipt)
            {
                if (sessionId.HasValue)
                {
                    base.SessionId = sessionId.Value;
                }
                base.State = state;
            }

        }
    }
}