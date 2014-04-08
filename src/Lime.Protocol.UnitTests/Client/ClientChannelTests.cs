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

        public ClientChannel GetTarget(bool autoReplyPings = true, bool autoNotifyReceipt = true)
        {
            return new ClientChannel(
                _transport.Object,
                _sendTimeout,
                autoReplyPings,
                autoNotifyReceipt
                );
        }

        #region SendNewSessionAsync

        //[TestMethod]
        //[TestCategory("StartNewSessionAsync")]
        //public async Task SendNewSessionAsync_NewState_CallsTransport()
        //{
        //    var target = GetTarget();

        //    await target.SendNewSessionAsync();

        //    _transport.Verify(
        //        t => t.SendAsync(It.Is<Session>(e => e.State == SessionState.New),
        //            It.IsAny<CancellationToken>()),
        //            Times.Once());
        //}

        //[TestMethod]
        //[TestCategory("SendNewSessionAsync")]
        //[ExpectedException(typeof(InvalidOperationException))]
        //public async Task SendNewSessionAsync_EstablishedState_ThrowsInvalidOperationException()
        //{
        //    var target = GetTarget();

        //    target.SetState(_transport, SessionState.Established);

        //    await target.SendNewSessionAsync();
        //}

        #endregion

        #region SendNegotiatingSessionAsync

        //[TestMethod]
        //[TestCategory("SendNegotiatingSessionAsync")]
        //public async Task SendNegotiatingSessionAsync_NegotiatingState_CallsTransport()
        //{
        //    var target = GetTarget(SessionState.Negotiating);

        //    var compression = SessionCompression.GZip;
        //    var encryption = SessionEncryption.TLS;

        //    await target.SendNegotiatingSessionAsync(compression, encryption);

        //    _transport.Verify(
        //        t => t.SendAsync(It.Is<Session>(
        //                e => e.State == SessionState.Negotiating &&
        //                     e.Id == target.SessionId &&
        //                     e.Compression == compression &&
        //                     e.Encryption == encryption),
        //            It.IsAny<CancellationToken>()),
        //            Times.Once());
        //}

        //[TestMethod]
        //[TestCategory("SendNegotiatingSessionAsync")]
        //[ExpectedException(typeof(InvalidOperationException))]
        //public async Task SendNegotiatingSessionAsync_NewState_ThrowsInvalidOperationException()
        //{
        //    var target = GetTarget(SessionState.New);

        //    var compression = SessionCompression.GZip;
        //    var encryption = SessionEncryption.TLS;

        //    await target.SendNegotiatingSessionAsync(compression, encryption);
        //}

        #endregion


        #region SendAuthenticatingSessionAsync

        //[TestMethod]
        //[TestCategory("SendAuthenticatingSessionAsync")]
        //public async Task SendAuthenticatingSessionAsync_AuthenticatingState_CallsTransport()
        //{
        //    var target = GetTarget();
        //    target.SetState(_transport, SessionState.Authenticating);

        //    var localIdentity = DataUtil.CreateIdentity();
        //    var localInstance = DataUtil.CreateInstanceName();
        //    var authentication = DataUtil.CreateAuthentication(Security.AuthenticationScheme.Plain);
        //    var sessionMode = SessionMode.Node;

        //    await target.SendAuthenticatingSessionAsync(
        //        localIdentity,
        //        authentication,
        //        localInstance,
        //        sessionMode);

        //    _transport.Verify(
        //        t => t.SendAsync(It.Is<Session>(
        //                e => e.State == SessionState.Authenticating &&
        //                     e.Id == target.SessionId &&
        //                     e.From.ToIdentity().Equals(localIdentity) &&
        //                     e.From.Instance.Equals(localInstance) &&
        //                     e.Mode == sessionMode &&
        //                     e.Authentication == authentication),
        //            It.IsAny<CancellationToken>()),
        //            Times.Once());
        //}

        //[TestMethod]
        //[TestCategory("SendAuthenticatingSessionAsync")]
        //[ExpectedException(typeof(InvalidOperationException))]
        //public async Task SendAuthenticatingSessionAsync_NewState_ThrowsInvalidOperationException()
        //{
        //    var target = GetTarget();

        //    var localIdentity = DataUtil.CreateIdentity();
        //    var localInstance = DataUtil.CreateInstanceName();
        //    var authentication = DataUtil.CreateAuthentication(Security.AuthenticationScheme.Plain);
        //    var sessionMode = SessionMode.Node;

        //    await target.SendAuthenticatingSessionAsync(
        //        localIdentity,
        //        authentication,
        //        localInstance,
        //        sessionMode);
        //}

        //[TestMethod]
        //[TestCategory("SendAuthenticatingSessionAsync")]
        //[ExpectedException(typeof(ArgumentNullException))]
        //public async Task SendAuthenticatingSessionAsync_NullIdentity_ThrowsArgumentNullException()
        //{
        //    var target = GetTarget();
        //    target.SetState(_transport, SessionState.Authenticating);

        //    Identity localIdentity = null;
        //    var localInstance = DataUtil.CreateInstanceName();
        //    var authentication = DataUtil.CreateAuthentication(Security.AuthenticationScheme.Plain);
        //    var sessionMode = SessionMode.Node;

        //    await target.SendAuthenticatingSessionAsync(
        //        localIdentity,
        //        authentication,
        //        localInstance,
        //        sessionMode);
        //}

        //[TestMethod]
        //[TestCategory("SendAuthenticatingSessionAsync")]
        //[ExpectedException(typeof(ArgumentNullException))]
        //public async Task SendAuthenticatingSessionAsync_NullAuthentication_ThrowsArgumentNullException()
        //{
        //    var target = GetTarget();
        //    target.SetState(_transport, SessionState.Authenticating);

        //    var localIdentity = DataUtil.CreateIdentity();
        //    var localInstance = DataUtil.CreateInstanceName();
        //    Authentication authentication = null;
        //    var sessionMode = SessionMode.Node;

        //    await target.SendAuthenticatingSessionAsync(
        //        localIdentity,
        //        authentication,
        //        localInstance,
        //        sessionMode);
        //}

        #endregion

        #region SendFinishingSessionAsync

        [TestMethod]
        [TestCategory("SendFinishingSessionAsync")]
        public async Task SendFinishingSessionAsync_EstablishedState_CallsTransport()
        {
            var target = GetTarget();

            target.SetState(_transport, SessionState.Established);

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
            var target = GetTarget();
            await target.SendFinishingSessionAsync();
        }

        #endregion

        #region SendReceivedNotificationAsync

        [TestMethod]
        [TestCategory("SendReceivedNotificationAsync")]
        public async Task SendReceivedNotificationAsync_EstablishedState_CallsTransport()
        {
            var target = GetTarget();
            target.SetState(_transport, SessionState.Established);

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
            var target = GetTarget();
            target.SetState(_transport, SessionState.Established);

            var message = DataUtil.CreateMessage(content: null);

            await target.SendReceivedNotificationAsync(message.Id.Value, null);
        }

        #endregion

        #region OnMessageReceivedAsync

        [TestMethod]
        [TestCategory("OnMessageReceivedAsync")]
        public void OnMessageReceivedAsync_MessageReceivedAndAutoNotifyReceiptTrue_SendsNotificationToTransport()
        {
            var target = GetTarget();
            target.SetState(_transport, SessionState.Established);

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            _transport.ReceiveEnvelope(message);

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
        public void OnMessageReceivedAsync_MessageReceivedAndAutoNotifyReceiptFalse_DoNotSendsNotificationToTransport()
        {
            var target = GetTarget(autoNotifyReceipt:false);
            target.SetState(_transport, SessionState.Established);

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            _transport.ReceiveEnvelope(message);

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
        public void OnMessageReceivedAsync_FireAndForgetMessageReceivedAndAutoNotifyReceiptTrue_DoNotSendsNotificationToTransport()
        {
            var target = GetTarget();
            target.SetState(_transport, SessionState.Established);

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            message.Id = null;
            _transport.ReceiveEnvelope(message);

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
        public void OnCommandReceivedAsync_PingCommandReceivedAndAutoReplyPingsTrue_SendsPingCommandToTransport()
        {
            var target = GetTarget();
            target.SetState(_transport, SessionState.Established);

            var ping = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(ping);
            _transport.ReceiveEnvelope(command);

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
        public void OnCommandReceivedAsync_PingCommandReceivedAndAutoReplyPingsFalse_DoNotSendsPingCommandToTransport()
        {
            var target = GetTarget(autoReplyPings: false);
            target.SetState(_transport, SessionState.Established);

            var ping = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(ping);
            _transport.ReceiveEnvelope(command);

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
        public void OnCommandReceivedAsync_PingResponseCommandReceivedAndAutoReplyPingsTrue_DoNotSendsPingCommandToTransport()
        {
            var target = GetTarget();
            target.SetState(_transport, SessionState.Established);

            var ping = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(ping, status: CommandStatus.Success);
            _transport.ReceiveEnvelope(command);

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

        //[TestMethod]
        //[TestCategory("OnSessionReceivedAsync")]
        //public void OnSessionReceivedAsync_EstablishedSessionReceived_SetsStateAndNodePropertiesAndRaisesSessionEstablished()
        //{
        //    bool sessionEstablishedRaised = false;

        //    var target = GetTarget();            
        //    target.SessionEstablished += (sender, e) => sessionEstablishedRaised = true;

        //    Assert.IsTrue(target.State == SessionState.New);
            
        //    var session = DataUtil.CreateSession();
        //    session.State = SessionState.Established;
        //    _transport.ReceiveEnvelope(session);

        //    Assert.IsTrue(target.State == session.State);
        //    Assert.IsTrue(target.LocalNode.Equals(session.To));
        //    Assert.IsTrue(target.RemoteNode.Equals(session.From));
        //    Assert.IsTrue(sessionEstablishedRaised);
        //}

        //[TestMethod]
        //[TestCategory("OnSessionReceivedAsync")]
        //public void OnSessionReceivedAsync_FinishedSessionReceived_SetsStateAndRaisesSessionFinished()
        //{
        //    bool sessionFinishedRaised = false;

        //    var target = GetTarget();
        //    target.SetState(_transport, SessionState.Established);
            
        //    target.SessionFinished += (sender, e) => sessionFinishedRaised = true;
            
        //    Assert.IsTrue(target.State == SessionState.Established);

        //    var session = DataUtil.CreateSession();
        //    session.State = SessionState.Finished;
        //    _transport.ReceiveEnvelope(session);

        //    Assert.IsTrue(target.State == session.State);
        //    Assert.IsTrue(sessionFinishedRaised);
        //}

        //[TestMethod]
        //[TestCategory("OnSessionReceivedAsync")]
        //public void OnSessionReceivedAsync_FailedSessionReceived_SetsStateAndRaisesSessionFailed()
        //{
        //    bool sessionFailedRaised = false;

        //    var session = DataUtil.CreateSession();
        //    session.State = SessionState.Failed;
        //    session.Reason = DataUtil.CreateReason();

        //    var target = GetTarget();
        //    target.SetState(_transport, SessionState.Established);

        //    target.SessionFailed += (sender, e) => sessionFailedRaised = e.Envelope == session;

        //    Assert.IsTrue(target.State == SessionState.Established);

        //    _transport.ReceiveEnvelope(session);

        //    Assert.IsTrue(target.State == session.State);
        //    Assert.IsTrue(sessionFailedRaised);
        //}

        #endregion
    }
}