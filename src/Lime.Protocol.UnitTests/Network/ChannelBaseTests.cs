using Lime.Protocol.Network;
using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Lime.Protocol.Util;

namespace Lime.Protocol.UnitTests.Network
{
    [TestFixture]
    public class ChannelBaseTests
    {
        #region Private fields

        private Mock<ITransport> _transport;
        private TimeSpan _sendTimeout;

        #endregion

        #region Scenario

        [SetUp]
        public void Setup()
        {
            _transport = new Mock<ITransport>();
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

        public ChannelBase GetTarget(SessionState state, int buffersLimit = 5, bool fillEnvelopeRecipients = false, Node remoteNode = null, Node localNode = null, bool autoReplyPings = false, TimeSpan? remotePingInterval = null, TimeSpan? remoteIdleTimeout = null)
        {
            return new TestChannel(
                state,
                _transport.Object,
                _sendTimeout,
                buffersLimit,
                fillEnvelopeRecipients,
                remoteNode, 
                localNode,
                autoReplyPings,
                remotePingInterval,
                remoteIdleTimeout
                );
        }

        #region SendMessageAsync

        [Test]
        [Category("SendMessageAsync")]
        public async Task SendMessageAsync_EstablishedState_CallsTransport()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var target = GetTarget(SessionState.Established);

            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);

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

        [Test]
        [Category("SendMessageAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendMessageAsync_NullMessage_ThrowsArgumentNullException()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var target = GetTarget(SessionState.Established);

            Message message = null;

            await target.SendMessageAsync(message);
        }

        [Test]
        [Category("SendMessageAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendMessageAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);

            await target.SendMessageAsync(message);
        }

        #endregion

        #region ReceiveMessageAsync

        [Test]
        [Category("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_EstablishedState_ReadsTransport()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(tcs.Task);
            var target = GetTarget(SessionState.Established);

            // Act
            var actual = await target.ReceiveMessageAsync(cancellationToken);

            // Assert
            Assert.AreEqual(message, actual);
            _transport.Verify();
        }

        [Test]
        [Category("ReceiveMessageAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveMessageAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);

            var cancellationToken = Dummy.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(message));

            var actual = await target.ReceiveMessageAsync(cancellationToken);
        }

        [Test]
        [Category("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_TransportDisconnect_ReadFromBufferThenThrow()
        {
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(Task.FromResult<Envelope>(message))
                .Throws<OperationCanceledException>();

            var target = GetTarget(SessionState.Established, 5);
            var cancellationToken = Dummy.CreateCancellationToken();

            await Task.Delay(200);

            Message messageReceived = null;

            Assert.DoesNotThrow(async () => messageReceived = await target.ReceiveMessageAsync(cancellationToken));
            Assert.IsNotNull(messageReceived);
            Assert.DoesNotThrow(async () => messageReceived = await target.ReceiveMessageAsync(cancellationToken));
            Assert.IsNotNull(messageReceived);
            Assert.Throws<OperationCanceledException>(async () => await target.ReceiveMessageAsync(cancellationToken));
        }

        [Test]
        [Category("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_NoRecipients_FillsFromTheSession()
        {
            var remoteNode = Dummy.CreateNode();
            var localNode = Dummy.CreateNode();
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            message.From = null;
            message.To = null;

            var cancellationToken = Dummy.CreateCancellationToken();
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
            Assert.AreEqual(localNode, message.To);
            Assert.AreEqual(remoteNode, message.From);
            Assert.IsNull(message.Pp);

            _transport.Verify();
        }


        [Test]
        [Category("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_IncompleteRecipients_FillsFromTheSession()
        {
            var remoteNode = Dummy.CreateNode();
            var localNode = Dummy.CreateNode();

            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            message.From = remoteNode.Copy();
            message.To = localNode.Copy();
            message.From.Domain = null;
            message.To.Domain = null;
            message.From.Instance = null;
            message.To.Instance = null;

            var cancellationToken = Dummy.CreateCancellationToken();

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

        [Test]
        [Category("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_ModuleReturnsMessage_ReturnsModuleMessage()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            var moduleMessage = Dummy.CreateMessage(content);

            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(tcs.Task);            
            var target = (TestChannel)GetTarget(SessionState.New);
            var moduleMock = new Mock<IChannelModule<Message>>();
            moduleMock
                .Setup(t => t.OnReceiving(message, It.IsAny<CancellationToken>()))
                .Returns(moduleMessage.AsCompletedTask());
            target.MessageModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveMessageAsync(cancellationToken);

            // Assert
            Assert.AreEqual(moduleMessage, actual);
            moduleMock.Verify(m => m.OnReceiving(message, It.IsAny<CancellationToken>()), Times.Once);
        }


        [Test]
        [Category("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_ModuleReturnsNull_IgnoresMessage()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var message1 = Dummy.CreateMessage(content);
            var message2 = Dummy.CreateMessage(content);
            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message1))
                .Returns(Task.FromResult<Envelope>(message2))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var moduleMock = new Mock<IChannelModule<Message>>();
            moduleMock
                .Setup(t => t.OnReceiving(message1, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Message>(null));
            moduleMock
                .Setup(t => t.OnReceiving(message2, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(message2));
            target.MessageModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveMessageAsync(cancellationToken);

            // Assert
            Assert.AreEqual(message2, actual);
            moduleMock.Verify(m => m.OnReceiving(message1, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region SendCommandAsync

        [Test]
        [Category("SendCommandAsync")]
        public async Task SendCommandAsync_EstablishedState_CallsTransport()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var target = GetTarget(SessionState.Established);

            var resource = Dummy.CreatePing();
            var command = Dummy.CreateCommand(resource);

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

        [Test]
        [Category("SendCommandAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendCommandAsync_NullCommand_ThrowsArgumentNullException()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var target = GetTarget(SessionState.Established);

            Command command = null;

            await target.SendCommandAsync(command);
        }

        [Test]
        [Category("SendCommandAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendCommandAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var content = Dummy.CreateTextContent();
            var command = Dummy.CreateCommand(content);

            await target.SendCommandAsync(command);
        }

        #endregion

        #region ReceiveCommandAsync

        [Test]
        [Category("ReceiveCommandAsync")]
        public async Task ReceiveCommandAsync_EstablishedState_ReadsTransport()
        {            
            var content = Dummy.CreateTextContent();
            var command = Dummy.CreateCommand(content);

            var cancellationToken = Dummy.CreateCancellationToken();

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

        [Test]
        [Category("ReceiveCommandAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveCommandAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var resource = Dummy.CreatePing();
            var command = Dummy.CreateCommand(resource);

            var cancellationToken = Dummy.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(command));

            var actual = await target.ReceiveCommandAsync(cancellationToken);
        }


        [Test]
        [Category("ReceiveCommandAsync")]
        public async Task ReceiveCommandAsync_ModuleReturnsCommand_ReturnsModuleCommand()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var command = Dummy.CreateCommand(content);
            var moduleCommand = Dummy.CreateCommand(content);

            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var moduleMock = new Mock<IChannelModule<Command>>();
            moduleMock
                .Setup(t => t.OnReceiving(command, It.IsAny<CancellationToken>()))
                .Returns(moduleCommand.AsCompletedTask());
            target.CommandModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveCommandAsync(cancellationToken);

            // Assert
            Assert.AreEqual(moduleCommand, actual);
            moduleMock.Verify(m => m.OnReceiving(command, It.IsAny<CancellationToken>()), Times.Once);
        }


        [Test]
        [Category("ReceiveCommandAsync")]
        public async Task ReceiveCommandAsync_ModuleReturnsNull_IgnoresCommand()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var command1 = Dummy.CreateCommand(content);
            var command2 = Dummy.CreateCommand(content);
            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(command1))
                .Returns(Task.FromResult<Envelope>(command2))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var moduleMock = new Mock<IChannelModule<Command>>();
            moduleMock
                .Setup(t => t.OnReceiving(command1, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Command>(null));
            moduleMock
                .Setup(t => t.OnReceiving(command2, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(command2));
            target.CommandModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveCommandAsync(cancellationToken);

            // Assert
            Assert.AreEqual(command2, actual);
            moduleMock.Verify(m => m.OnReceiving(command1, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region SendNotificationAsync

        [Test]
        [Category("SendNotificationAsync")]
        public async Task SendNotificationAsync_EstablishedState_CallsTransport()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var target = GetTarget(SessionState.Established);

            var notification = Dummy.CreateNotification(Event.Received);

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

        [Test]
        [Category("SendNotificationAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendNotificationAsync_NullNotification_ThrowsArgumentNullException()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var target = GetTarget(SessionState.Established);

            Notification notification = null;

            await target.SendNotificationAsync(notification);
        }

        [Test]
        [Category("SendNotificationAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendNotificationAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var notification = Dummy.CreateNotification(Event.Received);

            await target.SendNotificationAsync(notification);
        }

        #endregion

        #region ReceiveNotificationAsync

        [Test]
        [Category("ReceiveNotificationAsync")]
        public async Task ReceiveNotificationAsync_EstablishedState_ReadsTransport()
        {

            var notification = Dummy.CreateNotification(Event.Received);

            var cancellationToken = Dummy.CreateCancellationToken();

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

        [Test]
        [Category("ReceiveNotificationAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveNotificationAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var notification = Dummy.CreateNotification(Event.Received);

            var cancellationToken = Dummy.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(notification));

            var actual = await target.ReceiveNotificationAsync(cancellationToken);
        }


        [Test]
        [Category("ReceiveNotificationAsync")]
        public async Task ReceiveNotificationAsync_ModuleReturnsNotification_ReturnsModuleNotification()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Authorized);
            var moduleNotification = Dummy.CreateNotification(Event.Received);

            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(notification))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var moduleMock = new Mock<IChannelModule<Notification>>();
            moduleMock
                .Setup(t => t.OnReceiving(notification, It.IsAny<CancellationToken>()))
                .Returns(moduleNotification.AsCompletedTask());
            target.NotificationModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveNotificationAsync(cancellationToken);

            // Assert
            Assert.AreEqual(moduleNotification, actual);
            moduleMock.Verify(m => m.OnReceiving(notification, It.IsAny<CancellationToken>()), Times.Once);
        }


        [Test]
        [Category("ReceiveNotificationAsync")]
        public async Task ReceiveNotificationAsync_ModuleReturnsNull_IgnoresNotification()
        {
            // Arrange
            var notification1 = Dummy.CreateNotification(Event.Authorized);
            var notification2 = Dummy.CreateNotification(Event.Received);
            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(notification1))
                .Returns(Task.FromResult<Envelope>(notification2))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var moduleMock = new Mock<IChannelModule<Notification>>();
            moduleMock
                .Setup(t => t.OnReceiving(notification1, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Notification>(null));
            moduleMock
                .Setup(t => t.OnReceiving(notification2, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(notification2));
            target.NotificationModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveNotificationAsync(cancellationToken);

            // Assert
            Assert.AreEqual(notification2, actual);
            moduleMock.Verify(m => m.OnReceiving(notification1, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region SendSessionAsync

        [Test]
        [Category("SendSessionAsync")]
        public async Task SendSessionAsync_EstablishedState_CallsTransport()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var target = (ISessionChannel)GetTarget(SessionState.Established);

            var session = Dummy.CreateSession();

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

        [Test]
        [Category("SendSessionAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendSessionAsync_NullSession_ThrowsArgumentNullException()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var target = (ISessionChannel)GetTarget(SessionState.Established);

            Session session = null;

            await target.SendSessionAsync(session);
        }

        #endregion

        #region ReceiveSessionAsync

        [Test]
        [Category("ReceiveSessionAsync")]
        public async Task ReceiveSessionAsync_EstablishedState_ReadsTransport()
        {           
            var session = Dummy.CreateSession();

            var cancellationToken = Dummy.CreateCancellationToken();

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

        [Test]
        [Category("ReceiveSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveSessionAsync_FinishedState_ThrowsInvalidOperationException()
        {
            var target = (ISessionChannel)GetTarget(SessionState.Finished);

            var session = Dummy.CreateSession();

            var cancellationToken = Dummy.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session));

            var actual = await target.ReceiveSessionAsync(cancellationToken);
        }

        [Test]
        [Category("ReceiveSessionAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveSessionAsync_LimitedBuffers_ThrowsInvalidOperationException()
        {
            // Arrange
            var session = Dummy.CreateSession(SessionState.Finished);
            var cancellationToken = Dummy.CreateCancellationToken();            
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(session))
                .Returns(Task.FromResult<Envelope>(session));

            var target = (ISessionChannel)GetTarget(SessionState.Established, 1);
            await Task.Delay(100);

            // Act
            Assert.DoesNotThrow(async () => await target.ReceiveSessionAsync(cancellationToken));
            await target.ReceiveSessionAsync(cancellationToken);
        }

        #endregion

        #region EnvelopeAsyncBuffer_PromiseAdded

        [Test]
        [Category("EnvelopeAsyncBuffer_PromiseAdded")]
        [ExpectedException(typeof(InvalidOperationException))]

        public async Task EnvelopeAsyncBuffer_PromiseAdded_TransportThrowsException_CallsTransportCloseAsyncAndThrowsException()
        {
            var exception = Dummy.CreateException<InvalidOperationException>();
            var cancellationToken = Dummy.CreateCancellationToken();

            var taskCompletionSource = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(taskCompletionSource.Task)
                .Verifiable();

            _transport
                .Setup(t => t.CloseAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<object>(null))
                .Callback(() => _transport.Raise(t => t.Closing += (sender, e) => { }, new DeferralEventArgs()));

            var target = GetTarget(SessionState.Established);
            var receiveTask = target.ReceiveMessageAsync(cancellationToken);

            await Task.Delay(300);
            taskCompletionSource.SetException(exception);

            try
            {
                await receiveTask;
            }
            catch (InvalidOperationException ex)
            {
                _transport.Verify();

                _transport.Verify(
                    t => t.CloseAsync(It.IsAny<CancellationToken>()),
                    Times.Once());

                throw;
            }
        }

        [Test]
        [Category("EnvelopeAsyncBuffer_PromiseAdded")]
        public async Task EnvelopeAsyncBuffer_PromiseAdded_BufferHasPromises_ConsumersFromTransport()
        {
            // Arrange
            var cancellationToken = Dummy.CreateCancellationToken();

            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            var resource = Dummy.CreatePing();
            var command = Dummy.CreateCommand(resource);
            var notification = Dummy.CreateNotification(Event.Received);

            var session = Dummy.CreateSession();

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

            // Act
            var receiveMessageTask = target.ReceiveMessageAsync(cancellationToken);
            var receiveCommandTask = target.ReceiveCommandAsync(cancellationToken);
            var receiveNotificationTask = target.ReceiveNotificationAsync(cancellationToken);
            var receiveSessionTask = ((ISessionChannel)target).ReceiveSessionAsync(cancellationToken);

            // Assert
            await Task.WhenAll(receiveSessionTask, receiveNotificationTask, receiveCommandTask, receiveMessageTask);

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

        [Test]
        [Category("EnvelopeAsyncBuffer_PromiseAdded")]
        public async Task EnvelopeAsyncBuffer_PromiseAdded_BufferHasPromises_ConsumersFromTransportInverted()
        {
            var cancellationToken = Dummy.CreateCancellationToken();

            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            var resource = Dummy.CreatePing();
            var command = Dummy.CreateCommand(resource);
            var notification = Dummy.CreateNotification(Event.Received);

            var session = Dummy.CreateSession();

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

            // Act
            var receiveSessionTask = ((ISessionChannel)target).ReceiveSessionAsync(cancellationToken);
            var receiveNotificationTask = target.ReceiveNotificationAsync(cancellationToken);
            var receiveCommandTask = target.ReceiveCommandAsync(cancellationToken);
            var receiveMessageTask = target.ReceiveMessageAsync(cancellationToken);

            // Assert
            await Task.WhenAll(receiveSessionTask, receiveNotificationTask, receiveCommandTask, receiveMessageTask);

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

        [Test]
        [Category("Dispose")]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task Dispose_ReceiveMessageCalled_ThrowsTaskCancelledException()
        {
            var disposableTransport = _transport.As<IDisposable>();

            var cancellationToken = Dummy.CreateCancellationToken();
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
            
            await receiveMessageTask;
        }

        [Test]
        [Category("Dispose")]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task Dispose_ReceiveCommandCalled_ThrowsTaskCancelledException()
        {
            var disposableTransport = _transport.As<IDisposable>();

            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => tcs.Task);

            var target = GetTarget(SessionState.Established);
            var receiveCommandTask = target.ReceiveCommandAsync(cancellationToken);

            target.Dispose();

            disposableTransport.Verify(
                t => t.Dispose(),
                Times.Once());

            await receiveCommandTask;
        }

        [Test]
        [Category("Dispose")]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task Dispose_ReceiveNotificationCalled_ThrowsTaskCancelledException()
        {
            var disposableTransport = _transport.As<IDisposable>();

            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => tcs.Task);

            var target = GetTarget(SessionState.Established);
            var receiveNotificationTask = target.ReceiveNotificationAsync(cancellationToken);

            target.Dispose();

            disposableTransport.Verify(
                t => t.Dispose(),
                Times.Once());

            await receiveNotificationTask;
        }

        [Test]
        [Category("Dispose")]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task Dispose_ReceiveSessionCalled_ThrowsTaskCancelledException()
        {
            var disposableTransport = _transport.As<IDisposable>();

            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => tcs.Task);

            var target = GetTarget(SessionState.Established);
            var receiveSessionTask = target.ReceiveSessionAsync(cancellationToken);

            target.Dispose();

            disposableTransport.Verify(
                t => t.Dispose(),
                Times.Once());

            await receiveSessionTask;
        }


        [Test]
        [Category("PingRemoteAsync")]
        public async Task PingRemoteAsync_IdleChannel_SendsPing()
        {
            // Arrange
            var taskCompletionSource = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(taskCompletionSource.Task);
            var target = GetTarget(SessionState.Established, remotePingInterval: TimeSpan.FromMilliseconds(500));

            // Act
            await Task.Delay(1000);

            // Assert
            _transport
                .Verify(t => t.SendAsync(It.Is<Envelope>(e => e is Command && ((Command)e).Method == CommandMethod.Get && ((Command)e).Uri.ToString().Equals("/ping")), It.IsAny<CancellationToken>()), Times.AtLeastOnce);

            _transport
                .Verify(t => t.SendAsync(It.Is<Envelope>(e => e is Command && ((Command)e).Method == CommandMethod.Get && ((Command)e).Uri.ToString().Equals("/ping")), It.IsAny<CancellationToken>()), Times.AtMost(2));
        }

        [Test]
        [Category("PingRemoteAsync")]
        public async Task PingRemoteAsync_ActiveChannel_DoNotSendsPing()
        {
            // Arrange
            var envelope = Dummy.CreateMessage(Dummy.CreateTextContent());
            var tcs1 = new TaskCompletionSource<Envelope>();
            var tcs2 = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs1.Task)
                .Returns(tcs2.Task);
            var target = GetTarget(SessionState.Established, remotePingInterval: TimeSpan.FromMilliseconds(500));

            // Act
            await Task.Delay(350);
            tcs1.TrySetResult(envelope);
            await Task.Delay(350);

            // Assert
            _transport
                .Verify(t => t.SendAsync(It.Is<Envelope>(e => e is Command && ((Command)e).Method == CommandMethod.Get && ((Command)e).Uri.ToString().Equals("/ping")), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        [Category("PingRemoteAsync")]
        public async Task PingRemoteAsync_IdleChannel_RaiseOnRemotesIdle()
        {
            // Arrange
            var taskCompletionSource = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(taskCompletionSource.Task);
            var target = GetTarget(SessionState.Established, remotePingInterval: TimeSpan.FromMilliseconds(300), remoteIdleTimeout: TimeSpan.FromMilliseconds(500));

            // Act
            await Task.Delay(1000);

            // Assert
            _transport
                .Verify(t => t.SendAsync(It.Is<Envelope>(e => e is Command && ((Command)e).Method == CommandMethod.Get && ((Command)e).Uri.ToString().Equals("/ping")), It.IsAny<CancellationToken>()), Times.AtMostOnce);
        }


        [Test]
        [Category("PingRemoteAsync")]
        public async Task PingRemoteAsync_ActiveChannel_DoNotRaiseOnRemotesIdle()
        {
            // Arrange
            var envelope = Dummy.CreateMessage(Dummy.CreateTextContent());
            var tcs1 = new TaskCompletionSource<Envelope>();
            var tcs2 = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs1.Task)
                .Returns(tcs2.Task);
            var target = GetTarget(SessionState.Established, remotePingInterval: TimeSpan.FromMilliseconds(300), remoteIdleTimeout: TimeSpan.FromMilliseconds(500));

            // Act
            await Task.Delay(450);
            tcs1.TrySetResult(envelope);
            await Task.Delay(350);

            // Assert
            _transport
                .Verify(t => t.SendAsync(It.Is<Envelope>(e => e is Command && ((Command)e).Method == CommandMethod.Get && ((Command)e).Uri.ToString().Equals("/ping")), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            _transport
                .Verify(t => t.SendAsync(It.Is<Envelope>(e => e is Command && ((Command)e).Method == CommandMethod.Get && ((Command)e).Uri.ToString().Equals("/ping")), It.IsAny<CancellationToken>()), Times.AtMost(2));
        }

        #endregion

        #region Private classes

        private class TestChannel : ChannelBase
        {
            public TestChannel(SessionState state, ITransport transport, TimeSpan sendTimeout, int buffersLimit, bool fillEnvelopeRecipients, Node remoteNode = null, Node localNode = null, bool autoReplyPings = false, TimeSpan? remotePingInterval = null, TimeSpan? remoteIdleTimeout = null, int resendMessageTryCount = 0, TimeSpan? resendMessageInterval = null)
                : base(transport, sendTimeout, buffersLimit, fillEnvelopeRecipients, autoReplyPings, remotePingInterval, remoteIdleTimeout, resendMessageTryCount, resendMessageInterval)
            {                
                RemoteNode = remoteNode;
                LocalNode = localNode;
                State = state;
            }

            public void SetState(SessionState state)
            {
                State = state;
            }
        }

        #endregion
    }
}