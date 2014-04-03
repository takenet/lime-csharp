using Lime.Protocol.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.UnitTests.Network
{
    [TestClass]
    public class ChannelBaseTests
    {
        #region Private fields

        private Mock<ITransport> _transport;
        private TimeSpan _sendTimeout;

        #endregion

        #region Constructor

        public ChannelBaseTests()
        {
            _transport = new Mock<ITransport>();
            _sendTimeout = TimeSpan.FromSeconds(30);
        }

        #endregion

        public ChannelBase GetTarget(SessionState state)
        {
            return new TestChannel(
                state,
                _transport.Object,
                _sendTimeout
                );
        }

        #region SendNegotiatingSessionAsync

        [TestMethod]
        [TestCategory("SendNegotiatingSessionAsync")]
        public async Task SendNegotiatingSessionAsync_NegotiatingState_CallsTransport()
        {
            var target = GetTarget(SessionState.Negotiating);
            
            var compression = SessionCompression.GZip;
            var encryption = SessionEncryption.TLS;

            await target.SendNegotiatingSessionAsync(compression, encryption);

            _transport.Verify(
                t => t.SendAsync(It.Is<Session>(
                        e => e.State == SessionState.Negotiating &&
                             e.Id == target.SessionId &&
                             e.Compression == compression &&
                             e.Encryption == encryption),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [TestCategory("SendNegotiatingSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendNegotiatingSessionAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var compression = SessionCompression.GZip;
            var encryption = SessionEncryption.TLS;

            await target.SendNegotiatingSessionAsync(compression, encryption);
        }

        #endregion

        #region SendMessageAsync

        [TestMethod]
        [TestCategory("SendMessageAsync")]
        public async Task SendMessageAsync_EstablishedState_CallsTransport()
        {
            var target = GetTarget(SessionState.Established);

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            

            await target.SendMessageAsync(message);

            _transport.Verify(
                t => t.SendAsync(It.Is<Message>(
                        e => e.Id == message.Id &&
                             e.From.Equals(message.From) &&
                             e.To.Equals(message.To) &&
                             e.Content == message.Content),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [TestCategory("SendMessageAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendMessageAsync_NullMessage_ThrowsArgumentNullException()
        {
            var target = GetTarget(SessionState.Established);

            Message message = null;

            await target.SendMessageAsync(message);
        }

        [TestMethod]
        [TestCategory("SendMessageAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendMessageAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);            

            await target.SendMessageAsync(message);
        }

        #endregion

        #region SendCommandAsync

        [TestMethod]
        [TestCategory("SendCommandAsync")]
        public async Task SendCommandAsync_EstablishedState_CallsTransport()
        {
            var target = GetTarget(SessionState.Established);

            var resource = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(resource);

            await target.SendCommandAsync(command);

            _transport.Verify(
                t => t.SendAsync(It.Is<Command>(
                        e => e.Id == command.Id &&
                             e.From.Equals(command.From) &&
                             e.To.Equals(command.To) &&
                             e.Resource == command.Resource),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [TestCategory("SendCommandAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendCommandAsync_NullCommand_ThrowsArgumentNullException()
        {
            var target = GetTarget(SessionState.Established);

            Command command = null;

            await target.SendCommandAsync(command);
        }

        [TestMethod]
        [TestCategory("SendCommandAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendCommandAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var content = DataUtil.CreateTextContent();
            var command = DataUtil.CreateCommand(content);

            await target.SendCommandAsync(command);
        }

        #endregion

        #region SendNotificationAsync
        
        [TestMethod]
        [TestCategory("SendNotificationAsync")]
        public async Task SendNotificationAsync_EstablishedState_CallsTransport()
        {
            var target = GetTarget(SessionState.Established);

            var notification = DataUtil.CreateNotification(Event.Received);

            await target.SendNotificationAsync(notification);

            _transport.Verify(
                t => t.SendAsync(It.Is<Notification>(
                        e => e.Id == notification.Id &&
                             e.From.Equals(notification.From) &&
                             e.To.Equals(notification.To) &&
                             e.Event == notification.Event),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [TestCategory("SendNotificationAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendNotificationAsync_NullNotification_ThrowsArgumentNullException()
        {
            var target = GetTarget(SessionState.Established);

            Notification notification = null;

            await target.SendNotificationAsync(notification);
        }

        [TestMethod]
        [TestCategory("SendNotificationAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendNotificationAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var notification = DataUtil.CreateNotification(Event.Received);

            await target.SendNotificationAsync(notification);
        }

        #endregion

        #region OnMessageReceivedAsync
        
        [TestMethod]
        [TestCategory("OnMessageReceivedAsync")]
        public void OnMessageReceivedAsync_EstablishedState_RaiseMessageReceived()
        {
            var target = GetTarget(SessionState.Established);
            bool messageReceivedRaised = false;

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            target.MessageReceived += (sender, e) => messageReceivedRaised = e.Envelope == message;


            _transport.ReceiveEnvelope(message);

            Assert.IsTrue(messageReceivedRaised);
        }

        #endregion

        #region OnCommandReceivedAsync

        [TestMethod]
        [TestCategory("OnCommandReceivedAsync")]
        public void OnCommandReceivedAsync_EstablishedState_RaiseCommandReceived()
        {
            var target = GetTarget(SessionState.Established);
            bool commandReceivedRaised = false;

            var content = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(content);

            target.CommandReceived += (sender, e) => commandReceivedRaised = e.Envelope == command;

            _transport.ReceiveEnvelope(command);

            Assert.IsTrue(commandReceivedRaised);
        }

        #endregion

        #region OnNotificationReceivedAsync

        [TestMethod]
        [TestCategory("OnNotificationReceivedAsync")]
        public void OnNotificationReceivedAsync_EstablishedState_RaiseNotificationReceived()
        {
            var target = GetTarget(SessionState.Established);
            bool notificationReceivedRaised = false;

            var notification = DataUtil.CreateNotification(Event.Accepted);
            
            target.NotificationReceived += (sender, e) => notificationReceivedRaised = e.Envelope == notification;

            _transport.ReceiveEnvelope(notification);

            Assert.IsTrue(notificationReceivedRaised);
        }

        #endregion

        #region OnSessionReceivedAsync

        [TestMethod]
        [TestCategory("OnSessionReceivedAsync")]
        public void OnSessionReceivedAsync_EstablishedSession_RaiseSessionReceived()
        {
            var target = GetTarget(SessionState.Authenticating);
            bool sessionReceivedRaised = false;

            var session = DataUtil.CreateSession();
            session.State = SessionState.Established;

            target.SessionReceived += (sender, e) => sessionReceivedRaised = e.Envelope == session;

            _transport.ReceiveEnvelope(session);

            Assert.IsTrue(sessionReceivedRaised);
        }

        [TestMethod]
        [TestCategory("OnSessionReceivedAsync")]
        public void OnSessionReceivedAsync_NegotiatingSession_RaiseSessionReceivedAndNegotiatingSessionReceived()
        {
            var target = GetTarget(SessionState.Authenticating);
            bool sessionReceivedRaised = false;
            bool negotiatingSessionReceivedRaised = false;

            var session = DataUtil.CreateSession();
            session.State = SessionState.Negotiating;

            target.SessionReceived += (sender, e) => sessionReceivedRaised = e.Envelope == session;
            target.NegotiateSessionReceived += (sender, e) => negotiatingSessionReceivedRaised = e.Envelope == session;

            _transport.ReceiveEnvelope(session);

            Assert.IsTrue(sessionReceivedRaised);
            Assert.IsTrue(negotiatingSessionReceivedRaised);
        }


        [TestMethod]
        [TestCategory("OnSessionReceivedAsync")]
        public void OnSessionReceivedAsync_AuthenticatingSession_RaiseSessionReceivedAndAuthenticatingSessionReceived()
        {
            var target = GetTarget(SessionState.Authenticating);
            bool sessionReceivedRaised = false;
            bool authenticatingSessionReceivedRaised = false;

            var session = DataUtil.CreateSession();
            session.State = SessionState.Authenticating;

            target.SessionReceived += (sender, e) => sessionReceivedRaised = e.Envelope == session;
            target.AuthenticateSessionReceived += (sender, e) => authenticatingSessionReceivedRaised = e.Envelope == session;

            _transport.ReceiveEnvelope(session);

            Assert.IsTrue(sessionReceivedRaised);
            Assert.IsTrue(authenticatingSessionReceivedRaised);
        }

        #endregion

        #region Dispose

        [TestMethod]
        [TestCategory("Dispose")]
        public void Dispose_DisposeTransport()
        {
            var disposableTransport = _transport.As<IDisposable>();

            var target = GetTarget(SessionState.New);

            target.Dispose();

            disposableTransport.Verify(
                t => t.Dispose(),
                Times.Once());
        }

        #endregion

        #region Private classes

        private class TestChannel : ChannelBase
        {
            public TestChannel(SessionState state, ITransport transport, TimeSpan sendTimeout)
                : base(transport, sendTimeout)
            {
                base.State = state;
            }
        }

        #endregion
    }
}