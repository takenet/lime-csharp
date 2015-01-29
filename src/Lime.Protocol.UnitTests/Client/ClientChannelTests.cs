using System;
using Lime.Messaging.Resources;
using NUnit.Framework;
using System.Threading.Tasks;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Moq;
using System.Threading;
using Lime.Protocol.Security;
using Shouldly;

namespace Lime.Protocol.UnitTests.Client
{
    [TestFixture]
    public class ClientChannelTests
    {
        private Mock<ITransport> _transport;
        private TimeSpan _sendTimeout;

        #region Scenario

        [SetUp]
        public void Setup()
        {
            _transport = new Mock<ITransport>();
            _sendTimeout = TimeSpan.FromSeconds(30);
        }

        #endregion

        public ClientChannel GetTarget(Guid? sessionId = null, SessionState state = SessionState.New, bool fillEnvelopeRecipients = false, bool autoReplyPings = true, bool autoNotifyReceipt = true, Node remoteNode = null, Node localNode = null)
        {
            return new TestClientChannel(
                sessionId,
                state,
                _transport.Object,
                _sendTimeout,
                fillEnvelopeRecipients,
                autoReplyPings,
                autoNotifyReceipt,
                remoteNode,
                localNode
                );
        }

        #region StartNewSessionAsync

        [Test]
        [Category("StartNewSessionAsync")]
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

        [Test]
        [Category("SendNewSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task StartNewSessionAsync_InvalidState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(state: SessionState.Established);

            var cancellationToken = DataUtil.CreateCancellationToken();
            var actualSession = await target.StartNewSessionAsync(cancellationToken);
        }

        #endregion

        #region NegotiateSessionAsync

        [Test]
        [Category("NegotiateSessionAsync")]
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

            var actualSession = await target.NegotiateSessionAsync(compression, encryption, cancellationToken);

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

        [Test]
        [Category("NegotiateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task NegotiateSessionAsync_InvalidState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(state: SessionState.New);

            var cancellationToken = DataUtil.CreateCancellationToken();
            var compression = SessionCompression.GZip;
            var encryption = SessionEncryption.TLS;

            var actualSession = await target.NegotiateSessionAsync(compression, encryption, cancellationToken);
        }


  
        #endregion

        #region AuthenticateSessionAsync

        [Test]
        [Category("AuthenticateSessionAsync")]
        public async Task AuthenticateSessionAsync_AuthenticatingState_CallsTransportAndReadsFromTransport()
        {
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Authenticating);

            var cancellationToken = DataUtil.CreateCancellationToken();
            var localIdentity = DataUtil.CreateIdentity();
            var localInstance = DataUtil.CreateInstanceName();
            var authentication = DataUtil.CreateAuthentication(Security.AuthenticationScheme.Plain);

            var session = DataUtil.CreateSession(SessionState.Established);
            session.Id = target.SessionId;

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(session))
                .Returns(tcs.Task);

            var actualSession = await target.AuthenticateSessionAsync(localIdentity, authentication, localInstance, cancellationToken);

            _transport.Verify(
                t => t.SendAsync(It.Is<Session>(
                        e => e.State == SessionState.Authenticating &&
                                e.Id == target.SessionId &&
                                e.From.ToIdentity().Equals(localIdentity) &&
                                e.From.Instance.Equals(localInstance) &&
                                e.Authentication == authentication),
                    It.IsAny<CancellationToken>()),
                    Times.Once());            

            Assert.AreEqual(session, actualSession);
        }

        [Test]
        [Category("AuthenticateSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task AuthenticateSessionAsync_InvalidState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Established);

            var cancellationToken = DataUtil.CreateCancellationToken();
            var localIdentity = DataUtil.CreateIdentity();
            var localInstance = DataUtil.CreateInstanceName();
            var authentication = DataUtil.CreateAuthentication(Security.AuthenticationScheme.Plain);

            var actualSession = await target.AuthenticateSessionAsync(localIdentity, authentication, localInstance, cancellationToken);
        }

        [Test]
        [Category("AuthenticateSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AuthenticateSessionAsync_NullIdentity_ThrowsArgumentNullException()
        {
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Authenticating);

            var cancellationToken = DataUtil.CreateCancellationToken();
            Identity localIdentity = null;
            var localInstance = DataUtil.CreateInstanceName();
            var authentication = DataUtil.CreateAuthentication(Security.AuthenticationScheme.Plain);

            var actualSession = await target.AuthenticateSessionAsync(localIdentity, authentication, localInstance, cancellationToken);
        }

        [Test]
        [Category("AuthenticateSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AuthenticateSessionAsync_NullAuthentication_ThrowsArgumentNullException()
        {
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Authenticating);

            var cancellationToken = DataUtil.CreateCancellationToken();
            var localIdentity = DataUtil.CreateIdentity();
            var localInstance = DataUtil.CreateInstanceName();
            Authentication authentication = null;

            var actualSession = await target.AuthenticateSessionAsync(localIdentity, authentication, localInstance, cancellationToken);
        }


        #endregion

        #region SendReceivedNotificationAsync

        [Test]
        [Category("SendReceivedNotificationAsync")]
        public async Task SendReceivedNotificationAsync_EstablishedState_CallsTransport()
        {
            var target = GetTarget(state: SessionState.Established);

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            await target.SendReceivedNotificationAsync(message.Id, message.From);

            _transport.Verify(
                t => t.SendAsync(It.Is<Notification>(
                        n => n.Id == message.Id &&
                             n.To.Equals(message.From) &&
                             n.Event == Event.Received),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [Test]
        [Category("SendReceivedNotificationAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendReceivedNotificationAsync_NullTo_ThrowsArgumentNullException()
        {
            var target = GetTarget(state: SessionState.Established);

            var message = DataUtil.CreateMessage(content: null);

            await target.SendReceivedNotificationAsync(message.Id, null);
        }

        #endregion

        #region SendFinishingSessionAsync

        [Test]
        [Category("SendFinishingSessionAsync")]
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

        [Test]
        [Category("SendFinishingSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendFinishingSessionAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(state: SessionState.New); 
            await target.SendFinishingSessionAsync();
        }

        #endregion

        #region ReceiveSessionFinishedAsync

        [Test]
        [Category("ReceiveFinishedSessionAsync")]
        public async Task ReceiveFinishedSessionAsync_EstablishedState_ReadsTransport()
        {            
            var session = DataUtil.CreateSession(SessionState.Finished);            
            var tcs = new TaskCompletionSource<Envelope>();

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(session))
                .Returns(tcs.Task);

            var target = GetTarget(state: SessionState.Established);
            session.Id = target.SessionId;
            var actual = await target.ReceiveFinishedSessionAsync(cancellationToken);

            Assert.AreEqual(session, actual);
            _transport.Verify();
        }

        [Test]
        [Category("ReceiveFinishedSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveFinishedSessionAsync_InvalidState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(state: SessionState.Finished);

            var session = DataUtil.CreateSession();

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session));

            var actual = await target.ReceiveFinishedSessionAsync(cancellationToken);
        }

        #endregion

        #region SendMessageAsync

        [Test]
        [Category("SendMessageAsync")]
        public async Task SendMessageAsync_DelegateMessage_FillsFromTheSession()
        {
            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            var remoteNode = DataUtil.CreateNode();
            var localNode = DataUtil.CreateNode();

            var senderNode = DataUtil.CreateNode();
            var destinationNode = DataUtil.CreateNode();

            message.From = senderNode.Copy();
            message.To = destinationNode.Copy();

            var target = GetTarget(
                state: SessionState.Established,
                fillEnvelopeRecipients: true,
                remoteNode: remoteNode,
                localNode: localNode);


            await target.SendMessageAsync(message);

            _transport.Verify(
                t => t.SendAsync(It.Is<Message>(
                        e => e.Id == message.Id &&
                             e.From.Equals(senderNode) &&
                             e.To.Equals(destinationNode) &&
                             e.Pp != null &&
                             e.Pp.Equals(localNode) &&
                             e.Content == message.Content),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [Test]
        [Category("SendMessageAsync")]
        public async Task SendMessageAsync_DelegateMessageWithPpAndEmptyDomain_FillsFromTheSession()
        {
            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            var remoteNode = DataUtil.CreateNode();
            var localNode = DataUtil.CreateNode();

            var senderNode = DataUtil.CreateNode();
            var destinationNode = DataUtil.CreateNode();

            message.From = senderNode.Copy();
            message.To = destinationNode.Copy();
            message.Pp = localNode.Copy();
            message.Pp.Domain = null;

            var target = GetTarget(
                state: SessionState.Established,
                fillEnvelopeRecipients: true,
                remoteNode: remoteNode,
                localNode: localNode);


            await target.SendMessageAsync(message);

            _transport.Verify(
                t => t.SendAsync(It.Is<Message>(
                        e => e.Id == message.Id &&
                             e.From.Equals(senderNode) &&
                             e.To.Equals(destinationNode) &&
                             e.Pp != null &&
                             e.Pp.Equals(localNode) &&
                             e.Content == message.Content),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        #endregion

        #region ReceiveMessageAsync

        [Test]
        [Category("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_MessageReceivedAndAutoNotifyReceiptTrue_SendsNotificationToTransport()
        {
            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            var cts = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(cts.Task);

            var target = GetTarget(state: SessionState.Established, autoNotifyReceipt: true);

            var cancellationToken = DataUtil.CreateCancellationToken();
            var actual = await target.ReceiveMessageAsync(cancellationToken);

            _transport.Verify(
                t => t.SendAsync(It.Is<Notification>(
                        n => n.Id == message.Id &&
                             n.To.Equals(message.From) &&
                             n.Event == Event.Received),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [Test]
        [Category("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_MessageReceivedAndAutoNotifyReceiptFalse_DoNotSendsNotificationToTransport()
        {            
            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(tcs.Task);

            var target = GetTarget(state: SessionState.Established, autoNotifyReceipt: false);
            var actual = await target.ReceiveMessageAsync(cancellationToken);

            _transport.Verify(
                t => t.SendAsync(It.Is<Notification>(
                        n => n.Id == message.Id &&
                             n.To.Equals(message.From) &&
                             n.Event == Event.Received),
                    It.IsAny<CancellationToken>()),
                    Times.Never());
        }

        [Test]
        [Category("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_FireAndForgetMessageReceivedAndAutoNotifyReceiptTrue_DoNotSendsNotificationToTransport()
        {            
            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            message.Id = Guid.Empty;

            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(tcs.Task);

            var target = GetTarget(state: SessionState.Established, autoNotifyReceipt: true);
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

        #region ReceiveCommandAsync

        [Test]
        [Category("ReceiveCommandAsync")]
        public async Task ReceiveCommandAsync_PingCommandReceivedAndAutoReplyPingsTrue_SendsPingCommandToTransport()
        {           
            var ping = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(ping);
            command.Uri = LimeUri.Parse(UriTemplates.PING);
            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(tcs.Task);

            var target = GetTarget(state: SessionState.Established, autoReplyPings: true);
            
            await Task.Delay(100);

            _transport.Verify(
                t => t.SendAsync(It.Is<Command>(
                        c => c.Id == command.Id &&
                             c.To.Equals(command.From) &&
                             c.Resource.GetMediaType().ToString().Equals(Ping.MIME_TYPE, StringComparison.OrdinalIgnoreCase) &&
                             c.Status == CommandStatus.Success),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [Test]
        [Category("ReceiveCommandAsync")]
        public async Task ReceiveCommandAsync_PingCommandAbsoluteUriReceivedAndAutoReplyPingsTrue_SendsPingCommandToTransport()
        {
            var ping = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(ping);
            command.Uri = LimeUri.Parse(LimeUri.Parse(UriTemplates.PING).ToUri(command.From).ToString());
            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(tcs.Task);

            var target = GetTarget(state: SessionState.Established, autoReplyPings: true);

            await Task.Delay(100);

            _transport.Verify(
                t => t.SendAsync(It.Is<Command>(
                        c => c.Id == command.Id &&
                             c.To.Equals(command.From) &&
                             c.Resource.GetMediaType().ToString().Equals(Ping.MIME_TYPE, StringComparison.OrdinalIgnoreCase) &&
                             c.Status == CommandStatus.Success),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [Test]
        [Category("ReceiveCommandAsync")]
        public async Task ReceiveCommandAsync_PingCommandReceivedAndAutoReplyPingsFalse_DoNotSendsPingCommandToTransport()
        {           
            var ping = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(ping);
            command.Uri = LimeUri.Parse(UriTemplates.PING);
            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(tcs.Task);

            var target = GetTarget(state: SessionState.Established, autoReplyPings: false);
            var actual = await target.ReceiveCommandAsync(cancellationToken);

            actual.ShouldBe(command);

            _transport.Verify(
                t => t.SendAsync(It.Is<Command>(
                        c => c.Id == command.Id &&
                             c.To.Equals(command.From) &&
                             c.Resource is Ping &&
                             c.Status == CommandStatus.Success),
                    It.IsAny<CancellationToken>()),
                    Times.Never());
        }

        [Test]
        [Category("ReceiveCommandAsync")]
        public async Task ReceiveCommandAsync_PingResponseCommandReceivedAndAutoReplyPingsTrue_DoNotSendsPingCommandToTransport()
        {            
            var ping = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(ping, status: CommandStatus.Success);
            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(tcs.Task);

            var target = GetTarget(state: SessionState.Established, autoReplyPings: true);
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

        [Test]
        [Category("AuthenticateSessionAsync")]
        public async Task AuthenticateSessionAsync_AuthenticatingStateEstablishedSessionReceived_SetsStateAndNodeProperties()
        {

            var authentication = DataUtil.CreateAuthentication(AuthenticationScheme.Plain);
            var identity = DataUtil.CreateIdentity();
            var session = DataUtil.CreateSession(SessionState.Established);            
            var cancellationToken = DataUtil.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(session))
                .Returns(tcs.Task);
            
            var target = GetTarget(state: SessionState.Authenticating, autoNotifyReceipt: true);
            var actual = await target.AuthenticateSessionAsync(identity, authentication, null, cancellationToken);

            Assert.IsTrue(target.State == session.State);
            Assert.IsTrue(target.LocalNode.Equals(session.To));
            Assert.IsTrue(target.RemoteNode.Equals(session.From));
        }

        [Test]
        [Category("ReceiveFinishedSessionAsync")]
        public async Task ReceiveFinishedSessionAsync_EstablishedStateFinishedSessionReceived_SetsStateAndClosesTransport()
        {                                    
            var session = DataUtil.CreateSession(SessionState.Finished);
            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(session))
                .Returns(tcs.Task);

            var target = GetTarget(state: SessionState.Established, autoNotifyReceipt: true);
            var actual = await target.ReceiveFinishedSessionAsync(
                cancellationToken);

            Assert.IsTrue(target.State == session.State);

            _transport
                .Verify(t =>
                    t.CloseAsync(It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [Test]
        [Category("ReceiveFinishedSessionAsync")]
        public async Task ReceiveFinishedSessionAsync_EstablishedStateFailedSessionReceived_SetsStateAndClosesTransport()
        {
            
            var session = DataUtil.CreateSession(SessionState.Failed);
            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(session))
                .Returns(tcs.Task);
            
            var target = GetTarget(state: SessionState.Established, autoNotifyReceipt: true);
            var actual = await target.ReceiveFinishedSessionAsync(
                cancellationToken);

            Assert.IsTrue(target.State == session.State);

            _transport
                .Verify(t =>
                    t.CloseAsync(It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [Test]
        [Category("AuthenticateSessionAsync")]
        public async Task AuthenticateSessionAsync_AuthenticatingStateFailedSessionReceived_SetsStateAndClosesTransport()
        {            
            var authentication = DataUtil.CreateAuthentication(AuthenticationScheme.Plain);
            var identity = DataUtil.CreateIdentity();
            var session = DataUtil.CreateSession(SessionState.Failed);
            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var target = GetTarget(state: SessionState.Authenticating, autoNotifyReceipt: true);
            var actual = await target.AuthenticateSessionAsync(identity, authentication, null, cancellationToken);

            Assert.IsTrue(target.State == session.State);

            _transport
                .Verify(t =>
                    t.CloseAsync(It.IsAny<CancellationToken>()),
                    Times.Once());
        }
    
        #endregion

        private class TestClientChannel : ClientChannel
        {
            public TestClientChannel(Guid? sessionId, SessionState state, ITransport transport, TimeSpan sendTimeout, bool fillEnvelopeRecipients = false, bool autoReplyPings = true, bool autoNotifyReceipt = false, Node remoteNode = null, Node localNode = null)
                : base(transport, sendTimeout, 5, fillEnvelopeRecipients, autoReplyPings, autoNotifyReceipt)
            {
                if (sessionId.HasValue)
                {
                    base.SessionId = sessionId.Value;
                }
                base.State = state;
                base.RemoteNode = remoteNode;
                base.LocalNode = localNode;

            }

        }
    }
}