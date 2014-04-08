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

        #region ReceiveMessageAsync

        [TestMethod]
        [TestCategory("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_EstablishedState_ReadsTransport()
        {
            var target = GetTarget(SessionState.Established);

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(message))
                .Verifiable();

            var actual = await target.ReceiveMessageAsync(cancellationToken);

            Assert.AreEqual(message, actual);
            _transport.Verify();
        }

        [TestMethod]
        [TestCategory("ReceiveMessageAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveMessageAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(message));

            var actual = await target.ReceiveMessageAsync(cancellationToken);
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

        #region ReceiveCommandAsync

        [TestMethod]
        [TestCategory("ReceiveCommandAsync")]
        public async Task ReceiveCommandAsync_EstablishedState_ReadsTransport()
        {
            var target = GetTarget(SessionState.Established);

            var content = DataUtil.CreateTextContent();
            var command = DataUtil.CreateCommand(content);

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(command))
                .Verifiable();

            var actual = await target.ReceiveCommandAsync(cancellationToken);

            Assert.AreEqual(command, actual);
            _transport.Verify();
        }

        [TestMethod]
        [TestCategory("ReceiveCommandAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveCommandAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var content = DataUtil.CreateTextContent();
            var command = DataUtil.CreateCommand(content);

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(command));

            var actual = await target.ReceiveCommandAsync(cancellationToken);
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

        #region ReceiveNotificationAsync

        [TestMethod]
        [TestCategory("ReceiveNotificationAsync")]
        public async Task ReceiveNotificationAsync_EstablishedState_ReadsTransport()
        {
            var target = GetTarget(SessionState.Established);

            var notification = DataUtil.CreateNotification(Event.Received);

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(notification))
                .Verifiable();

            var actual = await target.ReceiveNotificationAsync(cancellationToken);

            Assert.AreEqual(notification, actual);
            _transport.Verify();
        }

        [TestMethod]
        [TestCategory("ReceiveNotificationAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveNotificationAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var notification = DataUtil.CreateNotification(Event.Received);

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(notification));

            var actual = await target.ReceiveNotificationAsync(cancellationToken);
        }

        #endregion

        #region SendSessionAsync

        [TestMethod]
        [TestCategory("SendSessionAsync")]
        public async Task SendSessionAsync_EstablishedState_CallsTransport()
        {
            var target = (ISessionChannel)GetTarget(SessionState.Established);

            var session = DataUtil.CreateSession();

            await target.SendSessionAsync(session);

            _transport.Verify(
                t => t.SendAsync(It.Is<Session>(
                        e => e.Id == session.Id &&
                             e.From.Equals(session.From) &&
                             e.To.Equals(session.To) &&
                             e.State == session.State),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [TestCategory("SendSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendSessionAsync_NullSession_ThrowsArgumentNullException()
        {
            var target = (ISessionChannel)GetTarget(SessionState.Established);

            Session session = null;

            await target.SendSessionAsync(session);
        }

        [TestMethod]
        [TestCategory("SendSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendSessionAsync_FinishedState_ThrowsInvalidOperationException()
        {
            var target = (ISessionChannel)GetTarget(SessionState.Finished);
            var session = DataUtil.CreateSession();

            await target.SendSessionAsync(session);
        }

        #endregion

        #region ReceiveSessionAsync

        [TestMethod]
        [TestCategory("ReceiveSessionAsync")]
        public async Task ReceiveSessionAsync_EstablishedState_ReadsTransport()
        {
            var target = (ISessionChannel)GetTarget(SessionState.Established);

            var session = DataUtil.CreateSession();

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session))
                .Verifiable();

            var actual = await target.ReceiveSessionAsync(cancellationToken);

            Assert.AreEqual(session, actual);
            _transport.Verify();
        }

        [TestMethod]
        [TestCategory("ReceiveSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveSessionAsync_FinishedState_ThrowsInvalidOperationException()
        {
            var target = (ISessionChannel)GetTarget(SessionState.Finished);

            var session = DataUtil.CreateSession();

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session));

            var actual = await target.ReceiveSessionAsync(cancellationToken);
        }

        #endregion

        #region EnvelopeAsyncBuffer_PromiseAdded

        [TestMethod]
        [TestCategory("EnvelopeAsyncBuffer_PromiseAdded")]
        public void EnvelopeAsyncBuffer_PromiseAdded_TransportThrowsException_CallsTransportCloseAsyncAndCancelTasks()
        {
            var target = GetTarget(SessionState.Established);

            var exception = DataUtil.CreateException();
            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception)
                .Verifiable();

            _transport
                .Setup(t => t.CloseAsync(It.IsAny<CancellationToken>()))
                .Callback(() => _transport.Raise(t => t.Closing += (sender, e) => { }, new DeferralEventArgs()));

            var receiveTask = target.ReceiveMessageAsync(cancellationToken);

            _transport.Verify();

            _transport.Verify(
                t => t.CloseAsync(It.IsAny<CancellationToken>()),
                Times.Once());

            Assert.IsTrue(receiveTask.IsCanceled);
        }

        [TestMethod]
        [TestCategory("EnvelopeAsyncBuffer_PromiseAdded")]
        public void EnvelopeAsyncBuffer_PromiseAdded_BufferHasPromises_ConsumersFromTransport()
        {
            var target = GetTarget(SessionState.Established);
            var cancellationToken = DataUtil.CreateCancellationToken();

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            var resource = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(resource);
            var notification = DataUtil.CreateNotification(Event.Received);

            var session = DataUtil.CreateSession();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(Task.FromResult<Envelope>(notification))
                .Returns(Task.FromResult<Envelope>(session))
                .Returns(Task.FromResult<Envelope>(message));

            var receiveMessageTask = target.ReceiveMessageAsync(cancellationToken);
            var receiveCommandTask = target.ReceiveCommandAsync(cancellationToken);
            var receiveNotificationTask = target.ReceiveNotificationAsync(cancellationToken);
            var receiveSessionTask = ((ISessionChannel)target).ReceiveSessionAsync(cancellationToken);
                                    
            Assert.IsTrue(receiveSessionTask.IsCompleted);
            Assert.IsTrue(receiveNotificationTask.IsCompleted);
            Assert.IsTrue(receiveCommandTask.IsCompleted);
            Assert.IsTrue(receiveMessageTask.IsCompleted);

            Assert.AreEqual(receiveSessionTask.Result, session);
            Assert.AreEqual(receiveNotificationTask.Result, notification);
            Assert.AreEqual(receiveCommandTask.Result, command);
            Assert.AreEqual(receiveMessageTask.Result, message);

            _transport.Verify(
                t => t.ReceiveAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));

        }

        [TestMethod]
        [TestCategory("EnvelopeAsyncBuffer_PromiseAdded")]
        public void EnvelopeAsyncBuffer_PromiseAdded_BufferHasPromises_ConsumersFromTransportInverted()
        {
            var target = GetTarget(SessionState.Established);
            var cancellationToken = DataUtil.CreateCancellationToken();

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            var resource = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(resource);
            var notification = DataUtil.CreateNotification(Event.Received);

            var session = DataUtil.CreateSession();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(Task.FromResult<Envelope>(notification))
                .Returns(Task.FromResult<Envelope>(session))
                .Returns(Task.FromResult<Envelope>(message));

            var receiveSessionTask = ((ISessionChannel)target).ReceiveSessionAsync(cancellationToken);
            var receiveNotificationTask = target.ReceiveNotificationAsync(cancellationToken);
            var receiveCommandTask = target.ReceiveCommandAsync(cancellationToken);
            var receiveMessageTask = target.ReceiveMessageAsync(cancellationToken);

            Assert.IsTrue(receiveSessionTask.IsCompleted);
            Assert.IsTrue(receiveNotificationTask.IsCompleted);
            Assert.IsTrue(receiveCommandTask.IsCompleted);
            Assert.IsTrue(receiveMessageTask.IsCompleted);

            Assert.AreEqual(receiveSessionTask.Result, session);
            Assert.AreEqual(receiveNotificationTask.Result, notification);
            Assert.AreEqual(receiveCommandTask.Result, command);
            Assert.AreEqual(receiveMessageTask.Result, message);

            _transport.Verify(
                t => t.ReceiveAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));

        }

        #endregion

        #region OnMessageReceivedAsync

        [TestMethod]
        [TestCategory("OnMessageReceivedAsync")]
        public async Task OnMessageReceivedAsync_NotEstablishedState_ClosesTransport()
        {
            var target = (TestChannel)GetTarget(SessionState.Authenticating);

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);           

            await target.CallsOnMessageReceivedAsync(message);
            
            _transport.Verify(
                t => t.CloseAsync(It.IsAny<CancellationToken>()),
                Times.Once());
        }

        #endregion

        #region OnCommandReceivedAsync

        [TestMethod]
        [TestCategory("OnCommandReceivedAsync")]
        public async Task OnCommandReceivedAsync_NotEstablishedState_ClosesTransport()
        {
            var target = (TestChannel)GetTarget(SessionState.Authenticating);

            var resource = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(resource);

            await target.CallsOnCommandReceivedAsync(command);

            _transport.Verify(
                t => t.CloseAsync(It.IsAny<CancellationToken>()),
                Times.Once());
        }

        #endregion

        #region OnNotificationReceivedAsync

        [TestMethod]
        [TestCategory("OnNotificationReceivedAsync")]
        public async Task OnNotificationReceivedAsync_NotEstablishedState_ClosesTransport()
        {
            var target = (TestChannel)GetTarget(SessionState.Authenticating);

            var notification = DataUtil.CreateNotification(Event.Received);

            await target.CallsOnNotificationReceivedAsync(notification);

            _transport.Verify(
                t => t.CloseAsync(It.IsAny<CancellationToken>()),
                Times.Once());
        }

        #endregion

        #region OnSessionReceivedAsync

        [TestMethod]
        [TestCategory("OnSessionReceivedAsync")]
        public void OnSessionReceivedAsync_FinishedState_DoesNothing()
        {
            var target = (TestChannel)GetTarget(SessionState.Authenticating);

            var session = DataUtil.CreateSession();

            var receiveSessionTask = target.CallsOnSessionReceivedAsync(session);

            Assert.IsTrue(receiveSessionTask.IsCompleted);
        }

        #endregion

        #region Dispose

        [TestMethod]
        [TestCategory("Dispose")]
        public void Dispose_Default_DisposeTransportAndCancelTasks()
        {
            var disposableTransport = _transport.As<IDisposable>();

            var target = GetTarget(SessionState.Established);
            var cancellationToken = DataUtil.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => tcs.Task);

            var receiveMessageTask = target.ReceiveMessageAsync(cancellationToken);

            target.Dispose();

            disposableTransport.Verify(
                t => t.Dispose(),
                Times.Once());

            Assert.IsTrue(receiveMessageTask.IsCanceled);
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

        #endregion
    }
}