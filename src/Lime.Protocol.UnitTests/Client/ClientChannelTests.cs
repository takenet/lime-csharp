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
using Lime.Protocol.Util;

namespace Lime.Protocol.UnitTests.Client
{
    [TestFixture]
    public class ClientChannelTests
    {
        private Mock<TransportBase> _transport;
        private TimeSpan _sendTimeout;

        #region Scenario

        [SetUp]
        public void Setup()
        {
            _transport = new Mock<TransportBase>();
            _transport
                .Setup(t => t.IsConnected)
                .Returns(true);
            _sendTimeout = TimeSpan.FromSeconds(30);
        }


        [TearDown]
        public void Teardown()
        {
            _transport = null;
            _sendTimeout = default(TimeSpan);
        }

        #endregion

        public ClientChannel GetTarget(Guid? sessionId = null, SessionState state = SessionState.New, bool fillEnvelopeRecipients = false, bool autoReplyPings = true, bool autoNotifyReceipt = true, Node remoteNode = null, Node localNode = null, TimeSpan? remotePingInterval = null, TimeSpan? remoteIdleTimeout = null)
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
                localNode,
                remotePingInterval,
                remoteIdleTimeout
                );
        }

        #region StartNewSessionAsync

        [Test]
        [Category("StartNewSessionAsync")]
        public async Task StartNewSessionAsync_NewState_CallsTransportAndReadsFromBuffer()
        {
            var target = GetTarget();            
            var cancellationToken = Dummy.CreateCancellationToken();

            var session = Dummy.CreateSession(SessionState.Authenticating);

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
        public async Task StartNewSessionAsync_InvalidState_ThrowsInvalidOperationException()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var target = GetTarget(state: SessionState.Established);

            var cancellationToken = Dummy.CreateCancellationToken();
            var actualSession =
                await target.StartNewSessionAsync(cancellationToken).ShouldThrowAsync<InvalidOperationException>();
        }

        #endregion

        #region NegotiateSessionAsync

        [Test]
        [Category("NegotiateSessionAsync")]
        public async Task NegotiateSessionAsync_NegotiatingState_CallsTransportAndReadsFromBuffer()
        {
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Negotiating);

            var cancellationToken = Dummy.CreateCancellationToken();
            var compression = SessionCompression.GZip;
            var encryption = SessionEncryption.TLS;

            var session = Dummy.CreateSession(SessionState.Negotiating);
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
        public async Task NegotiateSessionAsync_InvalidState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(state: SessionState.New);

            var cancellationToken = Dummy.CreateCancellationToken();
            var compression = SessionCompression.GZip;
            var encryption = SessionEncryption.TLS;

            var actualSession = await target.NegotiateSessionAsync(compression, encryption, cancellationToken).ShouldThrowAsync<InvalidOperationException>();
        }
  
        #endregion

        #region AuthenticateSessionAsync

        [Test]
        [Category("AuthenticateSessionAsync")]
        public async Task AuthenticateSessionAsync_AuthenticatingState_CallsTransportAndReadsFromTransport()
        {
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Authenticating);

            var cancellationToken = Dummy.CreateCancellationToken();
            var localIdentity = Dummy.CreateIdentity();
            var localInstance = Dummy.CreateInstanceName();
            var authentication = Dummy.CreateAuthentication(Security.AuthenticationScheme.Plain);

            var session = Dummy.CreateSession(SessionState.Established);
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
        public async Task AuthenticateSessionAsync_InvalidState_ThrowsInvalidOperationException()
        {
            // Arrange
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            var cancellationToken = Dummy.CreateCancellationToken();
            var localIdentity = Dummy.CreateIdentity();
            var localInstance = Dummy.CreateInstanceName();
            var authentication = Dummy.CreateAuthentication(Security.AuthenticationScheme.Plain);
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Established);
            
            // Act            
            var actualSession = await target
                .AuthenticateSessionAsync(localIdentity, authentication, localInstance, cancellationToken)
                .ShouldThrowAsync<InvalidOperationException>();
        }

        [Test]
        [Category("AuthenticateSessionAsync")]
        public async Task AuthenticateSessionAsync_NullIdentity_ThrowsArgumentNullException()
        {
            // Arrange
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Authenticating);
            var cancellationToken = Dummy.CreateCancellationToken();
            Identity localIdentity = null;
            var localInstance = Dummy.CreateInstanceName();
            var authentication = Dummy.CreateAuthentication(Security.AuthenticationScheme.Plain);

            // Act
            var actualSession = await target
                .AuthenticateSessionAsync(localIdentity, authentication, localInstance, cancellationToken)
                .ShouldThrowAsync<ArgumentNullException>();
        }

        [Test]
        [Category("AuthenticateSessionAsync")]
        public async Task AuthenticateSessionAsync_NullAuthentication_ThrowsArgumentNullException()
        {
            // Arrange
            var target = GetTarget(sessionId: Guid.NewGuid(), state: SessionState.Authenticating);
            var cancellationToken = Dummy.CreateCancellationToken();
            var localIdentity = Dummy.CreateIdentity();
            var localInstance = Dummy.CreateInstanceName();
            Authentication authentication = null;


            // Act
            var actualSession = await target
                .AuthenticateSessionAsync(localIdentity, authentication, localInstance, cancellationToken)
                .ShouldThrowAsync<ArgumentNullException>();
        }

        #endregion

        #region SendReceivedNotificationAsync

        [Test]
        [Category("SendReceivedNotificationAsync")]
        public async Task SendReceivedNotificationAsync_EstablishedState_CallsTransport()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var target = GetTarget(state: SessionState.Established);

            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);

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
        public void SendReceivedNotificationAsync_NullTo_ThrowsArgumentNullException()
        {
            // Arrange
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);            
            var target = GetTarget(state: SessionState.Established);
            var message = Dummy.CreateMessage(content: null);

            // Act
            Should.Throw<ArgumentNullException>(() => target.SendReceivedNotificationAsync(message.Id, null));
        }

        #endregion

        #region SendFinishingSessionAsync

        [Test]
        [Category("SendFinishingSessionAsync")]
        public async Task SendFinishingSessionAsync_EstablishedState_CallsTransport()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

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
        public void SendFinishingSessionAsync_NewState_ThrowsInvalidOperationException()
        {
            // Arrange
            var target = GetTarget(state: SessionState.New);
            
            // Act
            Should.Throw<InvalidOperationException>(() =>
                target.SendFinishingSessionAsync());
        }

        #endregion

        #region ReceiveSessionFinishedAsync

        [Test]
        [Category("ReceiveFinishedSessionAsync")]
        public async Task ReceiveFinishedSessionAsync_EstablishedState_ReadsTransport()
        {            
            var session = Dummy.CreateSession(SessionState.Finished);            
            var tcs = new TaskCompletionSource<Envelope>();

            var cancellationToken = Dummy.CreateCancellationToken();

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
        public void ReceiveFinishedSessionAsync_InvalidState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(state: SessionState.Finished);
            var session = Dummy.CreateSession();
            var cancellationToken = Dummy.CreateCancellationToken();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session));

            Should.Throw<InvalidOperationException>(() => target.ReceiveFinishedSessionAsync(cancellationToken));
        }

        #endregion

        #region SendMessageAsync

        [Test]
        [Category("SendMessageAsync")]
        public async Task SendMessageAsync_DelegateMessage_FillsFromTheSession()
        {
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);

            var remoteNode = Dummy.CreateNode();
            var localNode = Dummy.CreateNode();

            var senderNode = Dummy.CreateNode();
            var destinationNode = Dummy.CreateNode();

            message.From = senderNode.Copy();
            message.To = destinationNode.Copy();

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

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
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);

            var remoteNode = Dummy.CreateNode();
            var localNode = Dummy.CreateNode();

            var senderNode = Dummy.CreateNode();
            var destinationNode = Dummy.CreateNode();

            message.From = senderNode.Copy();
            message.To = destinationNode.Copy();
            message.Pp = localNode.Copy();
            message.Pp.Domain = null;

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

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
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);

            var cts = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(cts.Task);

            var target = GetTarget(state: SessionState.Established, autoNotifyReceipt: true);

            var cancellationToken = Dummy.CreateCancellationToken();
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
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);

            var cancellationToken = Dummy.CreateCancellationToken();

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
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            message.Id = Guid.Empty;

            var cancellationToken = Dummy.CreateCancellationToken();

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
            var ping = Dummy.CreatePing();
            var command = Dummy.CreateCommand(ping);
            command.Uri = LimeUri.Parse(UriTemplates.PING);
            var cancellationToken = Dummy.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(tcs.Task);

            var target = GetTarget(state: SessionState.Established, autoReplyPings: true);
            
            await Task.Delay(250);

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
            var ping = Dummy.CreatePing();
            var command = Dummy.CreateCommand(ping);
            command.Uri = LimeUri.Parse(LimeUri.Parse(UriTemplates.PING).ToUri(command.From).ToString());
            var cancellationToken = Dummy.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(tcs.Task);

            var target = GetTarget(state: SessionState.Established, autoReplyPings: true);

            await Task.Delay(250);

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
            var ping = Dummy.CreatePing();
            var command = Dummy.CreateCommand(ping);
            command.Uri = LimeUri.Parse(UriTemplates.PING);
            var cancellationToken = Dummy.CreateCancellationToken();

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
            var ping = Dummy.CreatePing();
            var command = Dummy.CreateCommand(ping, status: CommandStatus.Success);
            var cancellationToken = Dummy.CreateCancellationToken();

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

            var authentication = Dummy.CreateAuthentication(AuthenticationScheme.Plain);
            var identity = Dummy.CreateIdentity();
            var session = Dummy.CreateSession(SessionState.Established);            
            var cancellationToken = Dummy.CreateCancellationToken();
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
            var session = Dummy.CreateSession(SessionState.Finished);
            var cancellationToken = Dummy.CreateCancellationToken();

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
            
            var session = Dummy.CreateSession(SessionState.Failed);
            var cancellationToken = Dummy.CreateCancellationToken();

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
            var authentication = Dummy.CreateAuthentication(AuthenticationScheme.Plain);
            var identity = Dummy.CreateIdentity();
            var session = Dummy.CreateSession(SessionState.Failed);
            var cancellationToken = Dummy.CreateCancellationToken();

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


        [Test]
        [Category("OnRemoteIdleAsync")]
        public async Task OnRemoteIdleAsync_EstablishedState_CallsSendFinishingAndReceiveFinishedSessionAndClosesTransport()
        {
            // Arrange
            var session = Dummy.CreateSession(SessionState.Finished);            
            var tcs1 = new TaskCompletionSource<Envelope>();
            var tcs2 = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs1.Task)
                .Returns(tcs2.Task);
            _transport
                .Setup(t => t.SendAsync(It.Is<Envelope>(e => e is Session && ((Session)e).State == SessionState.Finishing), It.IsAny<CancellationToken>()))
                .Returns(() => TaskUtil.CompletedTask)
                .Callback(() => Task.Run(async () => 
                {
                    await Task.Delay(100);
                    tcs1.TrySetResult(session);
                }));

            var target = GetTarget(
                session.Id,
                SessionState.Established,
                remotePingInterval: TimeSpan.FromMilliseconds(100),
                remoteIdleTimeout: TimeSpan.FromMilliseconds(300));

            // Act
            await Task.Delay(1000);

            // Assert
            _transport
                .Verify(t =>
                    t.CloseAsync(It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        #endregion

        private class TestClientChannel : ClientChannel
        {
            public TestClientChannel(Guid? sessionId, SessionState state, ITransport transport, TimeSpan sendTimeout, bool fillEnvelopeRecipients = false, bool autoReplyPings = true, bool autoNotifyReceipt = false, Node remoteNode = null, Node localNode = null, TimeSpan? remotePingInterval = null, TimeSpan? remoteIdleTimeout = null)
                : base(transport, sendTimeout, 5, fillEnvelopeRecipients, autoReplyPings, autoNotifyReceipt, remotePingInterval, remoteIdleTimeout)
            {
                if (sessionId.HasValue)
                {
                    SessionId = sessionId.Value;
                }
                State = state;
                RemoteNode = remoteNode;
                LocalNode = localNode;
            }
        }
    }
}