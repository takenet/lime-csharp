using Lime.Protocol.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;

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

        public ChannelBase GetTarget(SessionState state, int buffersLimit = 5, bool fillEnvelopeRecipients = false, Node remoteNode = null, Node localNode = null)
        {
            return new TestChannel(
                state,
                _transport.Object,
                _sendTimeout,
                buffersLimit,
                fillEnvelopeRecipients,
                remoteNode, 
                localNode
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
                             e.Pp == null &&
                             e.Content == message.Content),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }

        [TestMethod]
        [TestCategory("SendMessageAsync")]
        public async Task SendMessageAsync_NoRecipients_FillsFromTheSession()
        {
            var remoteNode = DataUtil.CreateNode();
            var localNode = DataUtil.CreateNode();

            var target = GetTarget(
                SessionState.Established, 
                fillEnvelopeRecipients: true, 
                localNode: localNode, 
                remoteNode: remoteNode);

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            message.From = null;
            message.To = null;

            await target.SendMessageAsync(message);

            _transport.Verify(
                t => t.SendAsync(It.Is<Message>(
                        e => e.Id == message.Id &&
                             e.From.Equals(localNode) &&
                             e.To.Equals(remoteNode) &&
                             e.Pp == null &&
                             e.Content == message.Content),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
        }


        [TestMethod]
        [TestCategory("SendMessageAsync")]
        public async Task SendMessageAsync_IncompleteRecipients_FillsFromTheSession()
        {
            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            var localNode = message.From.Copy();
            var remoteNode = message.To.Copy();

            var target = GetTarget(
                SessionState.Established,
                fillEnvelopeRecipients: true,
                localNode: localNode,
                remoteNode: remoteNode);

            message.From.Domain = null;
            message.To.Domain = null;

            await target.SendMessageAsync(message);

            _transport.Verify(
                t => t.SendAsync(It.Is<Message>(
                        e => e.Id == message.Id &&
                             e.From.Equals(localNode) &&
                             e.To.Equals(remoteNode) &&
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
            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(tcs.Task);

            var target = GetTarget(SessionState.Established);
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

        [TestMethod]
        [TestCategory("ReceiveMessageAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ReceiveMessageAsync_LimitedBuffers_ThrowsInvalidOperationException()
        {
            int buffersLimit = 5;

            var cancellationToken = DataUtil.CreateCancellationToken();

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(Task.FromResult<Envelope>(message));

            var target = GetTarget(SessionState.Established, buffersLimit);

            var receiveMessageTask = target.ReceiveMessageAsync(cancellationToken);            
        }

        [TestMethod]
        [TestCategory("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_NoRecipients_FillsFromTheSession()
        {
            var remoteNode = DataUtil.CreateNode();
            var localNode = DataUtil.CreateNode();
            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            message.From = null;
            message.To = null;

            var cancellationToken = DataUtil.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(tcs.Task);

            var target = GetTarget(
                SessionState.Established,
                fillEnvelopeRecipients: true,
                remoteNode: remoteNode,
                localNode: localNode);

            var actual = await target.ReceiveMessageAsync(cancellationToken);

            Assert.AreEqual(message, actual);

            Assert.AreEqual(message.To, localNode);
            Assert.AreEqual(message.From, remoteNode);
            Assert.IsNull(message.Pp);

            _transport.Verify();
        }


        [TestMethod]
        [TestCategory("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_IncompleteRecipients_FillsFromTheSession()
        {
            var remoteNode = DataUtil.CreateNode();
            var localNode = DataUtil.CreateNode();

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            message.From = remoteNode.Copy();
            message.To = localNode.Copy();
            message.From.Domain = null;
            message.To.Domain = null;
            message.From.Instance = null;
            message.To.Instance = null;

            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(tcs.Task);

            var target = GetTarget(
                SessionState.Established,
                fillEnvelopeRecipients: true,
                remoteNode: remoteNode,
                localNode: localNode);

            var actual = await target.ReceiveMessageAsync(cancellationToken);

            Assert.AreEqual(message, actual);

            Assert.AreEqual(localNode.ToIdentity(), actual.To.ToIdentity());
            Assert.AreEqual(remoteNode.ToIdentity(), actual.From.ToIdentity());
            Assert.IsNull(actual.Pp);

            _transport.Verify();
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
            var content = DataUtil.CreateTextContent();
            var command = DataUtil.CreateCommand(content);

            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(tcs.Task);

            var target = GetTarget(SessionState.Established);
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

            var resource = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(resource);

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(command));

            var actual = await target.ReceiveCommandAsync(cancellationToken);
        }

        [TestMethod]
        [TestCategory("ReceiveCommandAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ReceiveCommandAsync_LimitedBuffers_ThrowsInvalidOperationException()
        {
            int buffersLimit = 5;

            var resource = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(resource);

            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(Task.FromResult<Envelope>(command));

            var target = GetTarget(SessionState.Established, buffersLimit);
            
            var receiveCommandTask = target.ReceiveCommandAsync(cancellationToken);            
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

            var notification = DataUtil.CreateNotification(Event.Received);

            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(notification))
                .Returns(tcs.Task);
            
            var target = GetTarget(SessionState.Established);
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

        [TestMethod]
        [TestCategory("ReceiveNotificationAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ReceiveNotificationAsync_LimitedBuffers_ThrowsInvalidOperationException()
        {
            int buffersLimit = 5;

            var notification = DataUtil.CreateNotification(Event.Received);

            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(notification))
                .Returns(Task.FromResult<Envelope>(notification))
                .Returns(Task.FromResult<Envelope>(notification))
                .Returns(Task.FromResult<Envelope>(notification))
                .Returns(Task.FromResult<Envelope>(notification))
                .Returns(Task.FromResult<Envelope>(notification));

            var target = GetTarget(SessionState.Established, buffersLimit);            
            var receiveNotificationTask = target.ReceiveNotificationAsync(cancellationToken);
            
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

        #endregion

        #region ReceiveSessionAsync

        [TestMethod]
        [TestCategory("ReceiveSessionAsync")]
        public async Task ReceiveSessionAsync_EstablishedState_ReadsTransport()
        {           
            var session = DataUtil.CreateSession();

            var cancellationToken = DataUtil.CreateCancellationToken();

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(session))
                .Returns(tcs.Task);

            var target = (ISessionChannel)GetTarget(SessionState.Established);
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

        [TestMethod]
        [TestCategory("ReceiveSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveSessionAsync_LimitedBuffers_ThrowsInvalidOperationException()
        {
            var session = DataUtil.CreateSession(SessionState.Finished);

            var cancellationToken = DataUtil.CreateCancellationToken();


            var taskCompletionSource = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(session))
                .Returns(Task.FromResult<Envelope>(session));

            var target = (ISessionChannel)GetTarget(SessionState.Established);

            var receiveSessionTask = await target.ReceiveSessionAsync(cancellationToken);            
        }

        #endregion

        #region EnvelopeAsyncBuffer_PromiseAdded

        [TestMethod]
        [TestCategory("EnvelopeAsyncBuffer_PromiseAdded")]
        public void EnvelopeAsyncBuffer_PromiseAdded_TransportThrowsException_CallsTransportCloseAsyncAndThrowsException()
        {
            var exception = DataUtil.CreateException<InvalidOperationException>();
            var cancellationToken = DataUtil.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception)
                .Verifiable();

            _transport
                .Setup(t => t.CloseAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<object>(null))
                .Callback(() => _transport.Raise(t => t.Closing += (sender, e) => { }, new DeferralEventArgs()));

            var target = GetTarget(SessionState.Established);

            try
            {
                var receiveTask = target.ReceiveMessageAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                _transport.Verify();

                _transport.Verify(
                    t => t.CloseAsync(It.IsAny<CancellationToken>()),
                    Times.Once());                              
            }
        }

        [TestMethod]
        [TestCategory("EnvelopeAsyncBuffer_PromiseAdded")]
        public void EnvelopeAsyncBuffer_PromiseAdded_BufferHasPromises_ConsumersFromTransport()
        {
            var cancellationToken = DataUtil.CreateCancellationToken();

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            var resource = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(resource);
            var notification = DataUtil.CreateNotification(Event.Received);

            var session = DataUtil.CreateSession();

            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(Task.FromResult<Envelope>(notification))
                .Returns(Task.FromResult<Envelope>(session))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(tcs.Task);
            
            var target = GetTarget(SessionState.Established);

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
                Times.AtLeast(4));

        }

        [TestMethod]
        [TestCategory("EnvelopeAsyncBuffer_PromiseAdded")]
        public void EnvelopeAsyncBuffer_PromiseAdded_BufferHasPromises_ConsumersFromTransportInverted()
        {
            var cancellationToken = DataUtil.CreateCancellationToken();

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            var resource = DataUtil.CreatePing();
            var command = DataUtil.CreateCommand(resource);
            var notification = DataUtil.CreateNotification(Event.Received);

            var session = DataUtil.CreateSession();

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(Task.FromResult<Envelope>(notification))
                .Returns(Task.FromResult<Envelope>(session))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(tcs.Task);

            var target = GetTarget(SessionState.Established);
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
                Times.AtLeast(4));

        }

        #endregion

        #region Dispose

        [TestMethod]
        [TestCategory("Dispose")]
        public void Dispose_Default_DisposeTransportAndCancelTasks()
        {
            var disposableTransport = _transport.As<IDisposable>();

            var cancellationToken = DataUtil.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => tcs.Task);

            var target = GetTarget(SessionState.Established);
            var receiveMessageTask = target.ReceiveMessageAsync(cancellationToken);

            target.Dispose();

            disposableTransport.Verify(
                t => t.Dispose(),
                Times.Once());

            receiveMessageTask.IsCanceled.ShouldBe(true);
        }

        #endregion

        #region Private classes

        private class TestChannel : ChannelBase
        {
            public TestChannel(SessionState state, ITransport transport, TimeSpan sendTimeout, int buffersLimit, bool fillEnvelopeRecipients, Node remoteNode = null, Node localNode = null)
                : base(transport, sendTimeout, buffersLimit, fillEnvelopeRecipients)
            {                
                base.RemoteNode = remoteNode;
                base.LocalNode = localNode;
                base.State = state;
            }
        }

        #endregion
    }
}