using Lime.Protocol.Network;
using NUnit.Framework;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Lime.Protocol.Util;

namespace Lime.Protocol.UnitTests.Network
{
    [TestFixture]
    public class ChannelBaseTests
    {
        private Mock<ITransport> _transport;
        private TimeSpan _sendTimeout;        

        [SetUp]
        public void SetUp()
        {
            _transport = new Mock<ITransport>();
            _transport
                .Setup(t => t.IsConnected)
                .Returns(true);
            _sendTimeout = TimeSpan.FromSeconds(30);
        }

        public ChannelBase GetTarget(SessionState state, int buffersLimit = 5, bool fillEnvelopeRecipients = false, Node remoteNode = null, Node localNode = null, bool autoReplyPings = false, TimeSpan? remotePingInterval = null, TimeSpan? remoteIdleTimeout = null, TimeSpan? consumeTimeout = null)
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
                remoteIdleTimeout,
                consumeTimeout: consumeTimeout
                );
        }

        #region SendMessageAsync

        [Test]
        [Category("SendMessageAsync")]
        public async Task SendMessageAsync_EstablishedState_CallsTransport()
        {
            // Arrange
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);            
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            var target = GetTarget(SessionState.Established);

            // Act
            await target.SendMessageAsync(message, CancellationToken.None);

            // Assert
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
        public async Task SendMessageAsync_NullMessage_ThrowsArgumentNullException()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var target = GetTarget(SessionState.Established);

            Message message = null;

            await target.SendMessageAsync(message, CancellationToken.None).ShouldThrowAsync<ArgumentNullException>();
        }

        [Test]
        [Category("SendMessageAsync")]
        public async Task SendMessageAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);

            await target.SendMessageAsync(message, CancellationToken.None).ShouldThrowAsync<InvalidOperationException>();
        }

        [Test]
        [Category("SendMessageAsync")]
        public async Task SendMessageAsync_ModuleReturnsMessage_SendsModuleMessage()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            var moduleMessage = Dummy.CreateMessage(content);
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var moduleMock = new Mock<IChannelModule<Message>>();
            moduleMock
                .Setup(t => t.OnSendingAsync(message, It.IsAny<CancellationToken>()))
                .Returns(moduleMessage.AsCompletedTask());
            target.MessageModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            await target.SendMessageAsync(message, CancellationToken.None);

            // Assert
            moduleMock.Verify(m => m.OnStateChanged(SessionState.Established), Times.Once());
            _transport.Verify(t => t.SendAsync(moduleMessage, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        [Category("SendMessageAsync")]
        public async Task SendMessageAsync_ModuleReturnsNull_DoNotCallTransport()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var moduleMock = new Mock<IChannelModule<Message>>();
            moduleMock
                .Setup(t => t.OnSendingAsync(message, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Message>(null));
            target.MessageModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            await target.SendMessageAsync(message, CancellationToken.None);

            // Assert
            moduleMock.Verify(m => m.OnStateChanged(SessionState.Established), Times.Once());
            _transport.Verify(t => t.SendAsync(message, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        [Category("SendMessageAsync")]
        public async Task SendMessageAsync_MultipleRegisteredModules_CallsEachModuleOnce()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var modulesCount = Dummy.CreateRandomInt(10) + 1;
            var modulesMockList = new List<Mock<IChannelModule<Message>>>();

            for (int i = 0; i < modulesCount; i++)
            {
                var moduleMock = new Mock<IChannelModule<Message>>();
                moduleMock
                    .Setup(t => t.OnSendingAsync(message, It.IsAny<CancellationToken>()))
                    .Returns(message.AsCompletedTask());
                target.MessageModules.Add(moduleMock.Object);
                modulesMockList.Add(moduleMock);
            }

            target.SetState(SessionState.Established);

            // Act
            await target.SendMessageAsync(message, CancellationToken.None);

            // Assert
            _transport.Verify(t => t.SendAsync(message, It.IsAny<CancellationToken>()), Times.Once());
            foreach (var mock in modulesMockList)
            {
                mock.Verify(m => m.OnSendingAsync(message, It.IsAny<CancellationToken>()), Times.Once());
            }
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
        public async Task ReceiveMessageAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);

            var cancellationToken = Dummy.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(message));

            var actual = await target.ReceiveMessageAsync(cancellationToken).ShouldThrowAsync<InvalidOperationException>();
        }

        [Test]
        [Category("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_TransportThrowsException_ReadFromBufferThenThrowException()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            var exception = Dummy.CreateException<ApplicationException>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message))
                .Returns(Task.FromResult<Envelope>(message))
                .Throws(exception);
            var target = GetTarget(SessionState.Established, 5);
            Exception actualException = null;
            target.ConsumerException += (sender, e) =>
            {
                using (e.GetDeferral())
                {
                    actualException = e.Exception;
                }
            };

            var cancellationToken = Dummy.CreateCancellationToken();
            
            // Act
            var messageReceived = await target.ReceiveMessageAsync(cancellationToken);
            Assert.IsNotNull(messageReceived);
            messageReceived = await target.ReceiveMessageAsync(cancellationToken);
            Assert.IsNotNull(messageReceived);

            // Assert
            target.ReceiveMessageAsync(cancellationToken).ShouldThrow<ApplicationException>();
            actualException.ShouldBe(actualException);
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
                .Setup(t => t.OnReceivingAsync(message, It.IsAny<CancellationToken>()))
                .Returns(moduleMessage.AsCompletedTask());
            target.MessageModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveMessageAsync(cancellationToken);

            // Assert
            Assert.AreEqual(moduleMessage, actual);
            moduleMock.Verify(m => m.OnReceivingAsync(message, It.IsAny<CancellationToken>()), Times.Once);
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
                .Setup(t => t.OnReceivingAsync(message1, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Message>(null));
            moduleMock
                .Setup(t => t.OnReceivingAsync(message2, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(message2));
            target.MessageModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveMessageAsync(cancellationToken);

            // Assert
            Assert.AreEqual(message2, actual);
            moduleMock.Verify(m => m.OnReceivingAsync(message1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_MultipleRegisteredModules_CallsEachModuleOnce()
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
            var target = (TestChannel)GetTarget(SessionState.New);
            var modulesCount = Dummy.CreateRandomInt(10) + 1;
            var modulesMockList = new List<Mock<IChannelModule<Message>>>();

            for (int i = 0; i < modulesCount; i++)
            {
                var moduleMock = new Mock<IChannelModule<Message>>();
                moduleMock
                    .Setup(t => t.OnReceivingAsync(message, It.IsAny<CancellationToken>()))
                    .Returns(message.AsCompletedTask());
                target.MessageModules.Add(moduleMock.Object);
                modulesMockList.Add(moduleMock);
            }
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveMessageAsync(cancellationToken);

            // Assert
            Assert.AreEqual(message, actual);
            foreach (var mock in modulesMockList)
            {
                mock.Verify(m => m.OnReceivingAsync(message, It.IsAny<CancellationToken>()), Times.Once());
            }
        }


        [Test]
        [Category("ReceiveMessageAsync")]
        public async Task ReceiveMessageAsync_ChannelNotBeingConsumer_ThrowsTimeoutException()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);

            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(message));

            var target = (TestChannel)GetTarget(SessionState.Established, buffersLimit: 1, consumeTimeout: TimeSpan.FromSeconds(1));

            // Act
            Exception exception = null;
            target.ConsumerException += (sender, e) =>
            {
                exception = e.Exception;
            };
            target.SetState(SessionState.Established);

            await Task.Delay(TimeSpan.FromSeconds(2));

            // Assert
            exception.ShouldNotBeNull();
            var timeoutException = exception.ShouldBeOfType<TimeoutException>();
            timeoutException.Message.ShouldBe("The transport consumer has timed out after 1 seconds. The receiver buffer has 1 (0/1) messages, 0 (0/0) notifications, 0 (0/0) commands, and 0 sessions and it may be the cause of the problem. Please ensure that the channel receive methods are being called.");
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

            await target.SendCommandAsync(command, CancellationToken.None);

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
        public async Task SendCommandAsync_ObserverCommand_CallsTransport()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var target = GetTarget(SessionState.Established);

            var resource = Dummy.CreatePing();
            var command = Dummy.CreateCommand();
            command.Id = null;
            command.Method = CommandMethod.Observe;

            await target.SendCommandAsync(command, CancellationToken.None);

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
        public async Task SendCommandAsync_NullCommand_ThrowsArgumentNullException()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var target = GetTarget(SessionState.Established);

            Command command = null;

            await target.SendCommandAsync(command, CancellationToken.None).ShouldThrowAsync<ArgumentNullException>();
        }

        [Test]
        [Category("SendCommandAsync")]        
        public async Task SendCommandAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var content = Dummy.CreateTextContent();
            var command = Dummy.CreateCommand(content);

            await target.SendCommandAsync(command, CancellationToken.None).ShouldThrowAsync<InvalidOperationException>();
        }

        [Test]
        [Category("SendCommandAsync")]
        public async Task SendCommandAsync_ModuleReturnsCommand_SendsModuleCommand()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var command = Dummy.CreateCommand(content);
            var moduleCommand = Dummy.CreateCommand(content);
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var moduleMock = new Mock<IChannelModule<Command>>();
            moduleMock
                .Setup(t => t.OnSendingAsync(command, It.IsAny<CancellationToken>()))
                .Returns(moduleCommand.AsCompletedTask());
            target.CommandModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            await target.SendCommandAsync(command, CancellationToken.None);

            // Assert
            moduleMock.Verify(m => m.OnSendingAsync(command, It.IsAny<CancellationToken>()), Times.Once());
            _transport.Verify(t => t.SendAsync(moduleCommand, It.IsAny<CancellationToken>()), Times.Once());            
        }

        [Test]
        [Category("SendCommandAsync")]
        public async Task SendCommandAsync_ModuleReturnsNull_DoNotCallTransport()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var command = Dummy.CreateCommand(content);

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var moduleMock = new Mock<IChannelModule<Command>>();
            moduleMock
                .Setup(t => t.OnSendingAsync(command, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Command>(null));
            target.CommandModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            await target.SendCommandAsync(command, CancellationToken.None);

            // Assert
            _transport.Verify(t => t.SendAsync(command, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        [Category("SendCommandAsync")]
        public async Task SendCommandAsync_MultipleRegisteredModules_CallsEachModuleOnce()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var command = Dummy.CreateCommand(content);
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var modulesCount = Dummy.CreateRandomInt(10) + 1;
            var modulesMockList = new List<Mock<IChannelModule<Command>>>();

            for (int i = 0; i < modulesCount; i++)
            {
                var moduleMock = new Mock<IChannelModule<Command>>();
                moduleMock
                    .Setup(t => t.OnSendingAsync(command, It.IsAny<CancellationToken>()))
                    .Returns(command.AsCompletedTask());
                target.CommandModules.Add(moduleMock.Object);
                modulesMockList.Add(moduleMock);
            }

            target.SetState(SessionState.Established);

            // Act
            await target.SendCommandAsync(command, CancellationToken.None);

            // Assert
            _transport.Verify(t => t.SendAsync(command, It.IsAny<CancellationToken>()), Times.Once());
            foreach (var mock in modulesMockList)
            {
                mock.Verify(m => m.OnSendingAsync(command, It.IsAny<CancellationToken>()), Times.Once());
            }
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
        public async Task ReceiveCommandAsync_ObserveCommand_ReadsTransport()
        {
            var command = Dummy.CreateCommand();
            command.Id = null;
            command.Method = CommandMethod.Observe;

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
        public async Task ReceiveCommandAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var resource = Dummy.CreatePing();
            var command = Dummy.CreateCommand(resource);

            var cancellationToken = Dummy.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(command));

            var actual = await target.ReceiveCommandAsync(cancellationToken).ShouldThrowAsync<InvalidOperationException>();
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
                .Setup(t => t.OnReceivingAsync(command, It.IsAny<CancellationToken>()))
                .Returns(moduleCommand.AsCompletedTask());
            target.CommandModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveCommandAsync(cancellationToken);

            // Assert
            Assert.AreEqual(moduleCommand, actual);
            moduleMock.Verify(m => m.OnReceivingAsync(command, It.IsAny<CancellationToken>()), Times.Once);
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
                .Setup(t => t.OnReceivingAsync(command1, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Command>(null));
            moduleMock
                .Setup(t => t.OnReceivingAsync(command2, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(command2));
            target.CommandModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveCommandAsync(cancellationToken);

            // Assert
            Assert.AreEqual(command2, actual);
            moduleMock.Verify(m => m.OnReceivingAsync(command1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("ReceiveCommandAsync")]
        public async Task ReceiveCommandAsync_MultipleRegisteredModules_CallsEachModuleOnce()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var command = Dummy.CreateCommand(content);

            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(command))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var modulesCount = Dummy.CreateRandomInt(10) + 1;
            var modulesMockList = new List<Mock<IChannelModule<Command>>>();

            for (int i = 0; i < modulesCount; i++)
            {
                var moduleMock = new Mock<IChannelModule<Command>>();
                moduleMock
                    .Setup(t => t.OnReceivingAsync(command, It.IsAny<CancellationToken>()))
                    .Returns(command.AsCompletedTask());
                target.CommandModules.Add(moduleMock.Object);
                modulesMockList.Add(moduleMock);
            }
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveCommandAsync(cancellationToken);

            // Assert
            Assert.AreEqual(command, actual);
            foreach (var mock in modulesMockList)
            {
                mock.Verify(m => m.OnReceivingAsync(command, It.IsAny<CancellationToken>()), Times.Once());
            }            
        }

        #endregion

        #region ProcessCommandAsync

        [Test]
        [Category("ProcessCommandAsync")]
        public async Task ProcessCommandAsync_SingleCommand_SendToTransportAndAwaitsForResponse()
        {
            // Arrange            
            var requestCommand = Dummy.CreateCommand();
            var cancellationToken = Dummy.CreateCancellationToken(TimeSpan.FromSeconds(5));
            var responseCommand = Dummy.CreateCommand(Dummy.CreatePing(), status: CommandStatus.Success);
            responseCommand.Id = requestCommand.Id;
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.SendAsync(It.IsAny<Command>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    tcs.SetResult(responseCommand);
                    return TaskUtil.CompletedTask;
                });                
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);            
            var target = GetTarget(SessionState.Established);


            // Act
            var actual = await target.ProcessCommandAsync(requestCommand, cancellationToken);

            // Assert
            _transport.Verify(
                t => t.SendAsync(requestCommand,
                    It.IsAny<CancellationToken>()),
                    Times.Once());
            actual.ShouldBe(responseCommand);            
        }


        [Test]
        [Category("ProcessCommandAsync")]
        public async Task ProcessCommandAsync_MultipleCommands_SendToTransportAndAwaitsForResponse()
        {
            // Arrange            
            var count = Dummy.CreateRandomInt(100) + 1;
            var requestCommands = new List<Command>();
            var responseCommands = new Dictionary<string, Command>();

            for (int i = 0; i < count; i++)
            {
                var requestCommand = Dummy.CreateCommand();
                var responseCommand = Dummy.CreateCommand(Dummy.CreatePing(), status: CommandStatus.Success);
                responseCommand.Id = requestCommand.Id;
                requestCommands.Add(requestCommand);
                responseCommands.Add(requestCommand.Id, responseCommand);
            }
            
            var cancellationToken = Dummy.CreateCancellationToken(TimeSpan.FromSeconds(5));
            var responseQueue = new BlockingCollection<Command>();
            _transport
                .Setup(t => t.SendAsync(It.Is<Command>(c => requestCommands.Contains(c)), It.IsAny<CancellationToken>()))
                .Returns((Command c, CancellationToken t) =>
                {
                    responseQueue.Add(responseCommands[c.Id]);                    
                    return TaskUtil.CompletedTask;
                });
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken t) => ((Envelope)responseQueue.Take(t)).AsCompletedTask());
            var target = GetTarget(SessionState.Established);
            var actuals = new List<Command>();

            // Act
            foreach (var requestCommand in requestCommands)
            {
                actuals.Add(
                    await target.ProcessCommandAsync(requestCommand, cancellationToken));
            }

            // Assert
            foreach (var requestCommand in requestCommands)
            {
                _transport.Verify(
                    t => t.SendAsync(requestCommand,
                        It.IsAny<CancellationToken>()),
                        Times.Once());                
            }

            foreach (var responseCommand in responseCommands)
            {
                actuals.ShouldContain(responseCommand.Value);
            }
        }

        [Test]
        [Category("ProcessCommandAsync")]        
        public async Task ProcessCommandAsync_DuplicatedIdWhileResponseNotReceived_ThrowsInvalidOperationException()
        {
            // Arrange            
            var requestCommand = Dummy.CreateCommand();
            var cancellationToken = Dummy.CreateCancellationToken(TimeSpan.FromSeconds(5));
            
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            var target = GetTarget(SessionState.Established);
            var processingTask = target.ProcessCommandAsync(requestCommand, cancellationToken);

            // Act
            await target.ProcessCommandAsync(requestCommand, cancellationToken).ShouldThrowAsync<InvalidOperationException>();
        }

        [Test]
        [Category("ProcessCommandAsync")]
        public async Task ProcessCommandAsync_DuplicatedIdWhileFirstResponseReceived_SendsToBuffer()
        {
            // Arrange            
            var requestCommand = Dummy.CreateCommand();
            var cancellationToken = Dummy.CreateCancellationToken(TimeSpan.FromSeconds(2));
            var responseCommand = Dummy.CreateCommand(Dummy.CreatePing(), status: CommandStatus.Success);
            responseCommand.Id = requestCommand.Id;
            var tcs1 = new TaskCompletionSource<Envelope>();
            var tcs2 = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.SendAsync(It.IsAny<Command>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    tcs1.TrySetResult(responseCommand);
                    return TaskUtil.CompletedTask;
                });
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs1.Task)
                .Returns(tcs1.Task)
                .Returns(tcs2.Task);
            var target = GetTarget(SessionState.Established);
            await target.ProcessCommandAsync(requestCommand, cancellationToken);

            // Act
            var actual = await target.ReceiveCommandAsync(cancellationToken);
            var processingTask = target.ProcessCommandAsync(requestCommand, cancellationToken);
            
            // Assert
            _transport.Verify(
                t => t.SendAsync(requestCommand,
                    It.IsAny<CancellationToken>()),
                    Times.Exactly(2));
            actual.ShouldBe(responseCommand);

            Exception exception = null;
            try
            {
                await processingTask;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            exception.ShouldNotBeNull();
            exception.ShouldBeOfType<TaskCanceledException>();
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

            await target.SendNotificationAsync(notification, CancellationToken.None);

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
        public async Task SendNotificationAsync_NullNotification_ThrowsArgumentNullException()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            var target = GetTarget(SessionState.Established);

            Notification notification = null;

            await target.SendNotificationAsync(notification, CancellationToken.None).ShouldThrowAsync<ArgumentNullException>();
        }

        [Test]
        [Category("SendNotificationAsync")]
        public async Task SendNotificationAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var notification = Dummy.CreateNotification(Event.Received);

            await target.SendNotificationAsync(notification, CancellationToken.None).ShouldThrowAsync<InvalidOperationException>();
        }

        [Test]
        [Category("SendNotificationAsync")]
        public async Task SendNotificationAsync_ModuleReturnsNotification_SendsModuleNotification()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            var moduleNotification = Dummy.CreateNotification(Event.Received);
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var moduleMock = new Mock<IChannelModule<Notification>>();
            moduleMock
                .Setup(t => t.OnSendingAsync(notification, It.IsAny<CancellationToken>()))
                .Returns(moduleNotification.AsCompletedTask());
            target.NotificationModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            await target.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            _transport.Verify(t => t.SendAsync(moduleNotification, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        [Category("SendNotificationAsync")]
        public async Task SendNotificationAsync_ModuleReturnsNull_DoNotCallTransport()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);

            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var moduleMock = new Mock<IChannelModule<Notification>>();
            moduleMock
                .Setup(t => t.OnSendingAsync(notification, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Notification>(null));
            target.NotificationModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            await target.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            _transport.Verify(t => t.SendAsync(notification, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        [Category("SendNotificationAsync")]
        public async Task SendNotificationAsync_MultipleRegisteredModules_CallsEachModuleOnce()
        {
            // Arrange            
            var notification = Dummy.CreateNotification(Event.Received);
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var modulesCount = Dummy.CreateRandomInt(10) + 1;
            var modulesMockList = new List<Mock<IChannelModule<Notification>>>();

            for (int i = 0; i < modulesCount; i++)
            {
                var moduleMock = new Mock<IChannelModule<Notification>>();
                moduleMock
                    .Setup(t => t.OnSendingAsync(notification, It.IsAny<CancellationToken>()))
                    .Returns(notification.AsCompletedTask());
                target.NotificationModules.Add(moduleMock.Object);
                modulesMockList.Add(moduleMock);
            }

            target.SetState(SessionState.Established);

            // Act
            await target.SendNotificationAsync(notification, CancellationToken.None);
            await Task.Delay(250);

            // Assert
            _transport.Verify(t => t.SendAsync(notification, It.IsAny<CancellationToken>()), Times.Once());
            foreach (var mock in modulesMockList)
            {
                mock.Verify(m => m.OnSendingAsync(notification, It.IsAny<CancellationToken>()), Times.Once());
            }
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
        public async Task ReceiveNotificationAsync_NewState_ThrowsInvalidOperationException()
        {
            var target = GetTarget(SessionState.New);

            var notification = Dummy.CreateNotification(Event.Received);

            var cancellationToken = Dummy.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(notification));

            var actual = await target.ReceiveNotificationAsync(cancellationToken).ShouldThrowAsync<InvalidOperationException>();
        }


        [Test]
        [Category("ReceiveNotificationAsync")]
        public async Task ReceiveNotificationAsync_ModuleReturnsNotification_ReturnsModuleNotification()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
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
                .Setup(t => t.OnReceivingAsync(notification, It.IsAny<CancellationToken>()))
                .Returns(moduleNotification.AsCompletedTask());
            target.NotificationModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveNotificationAsync(cancellationToken);

            // Assert
            Assert.AreEqual(moduleNotification, actual);
            moduleMock.Verify(m => m.OnReceivingAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        }


        [Test]
        [Category("ReceiveNotificationAsync")]
        public async Task ReceiveNotificationAsync_ModuleReturnsNull_IgnoresNotification()
        {
            // Arrange
            var notification1 = Dummy.CreateNotification(Event.Consumed);
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
                .Setup(t => t.OnReceivingAsync(notification1, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Notification>(null));
            moduleMock
                .Setup(t => t.OnReceivingAsync(notification2, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(notification2));
            target.NotificationModules.Add(moduleMock.Object);
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveNotificationAsync(cancellationToken);

            // Assert
            Assert.AreEqual(notification2, actual);
            moduleMock.Verify(m => m.OnReceivingAsync(notification1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("ReceiveNotificationAsync")]
        public async Task ReceiveNotificationAsync_MultipleRegisteredModules_CallsEachModuleOnce()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);

            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .SetupSequence(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Envelope>(notification))
                .Returns(tcs.Task);
            var target = (TestChannel)GetTarget(SessionState.New);
            var modulesCount = Dummy.CreateRandomInt(10) + 1;
            var modulesMockList = new List<Mock<IChannelModule<Notification>>>();

            for (int i = 0; i < modulesCount; i++)
            {
                var moduleMock = new Mock<IChannelModule<Notification>>();
                moduleMock
                    .Setup(t => t.OnReceivingAsync(notification, It.IsAny<CancellationToken>()))
                    .Returns(notification.AsCompletedTask());
                target.NotificationModules.Add(moduleMock.Object);
                modulesMockList.Add(moduleMock);
            }
            target.SetState(SessionState.Established);

            // Act
            var actual = await target.ReceiveNotificationAsync(cancellationToken);

            // Assert
            Assert.AreEqual(notification, actual);
            foreach (var mock in modulesMockList)
            {
                mock.Verify(m => m.OnReceivingAsync(notification, It.IsAny<CancellationToken>()), Times.Once());
            }
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

            await target.SendSessionAsync(session, CancellationToken.None);

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
        public void SendSessionAsync_NullSession_ThrowsArgumentNullException()
        {
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            var target = (ISessionChannel)GetTarget(SessionState.Established);
            Session session = null;

            Should.Throw<ArgumentNullException>(() => target.SendSessionAsync(session, CancellationToken.None));
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
        public async Task ReceiveSessionAsync_FinishedState_ThrowsInvalidOperationException()
        {
            var target = (ISessionChannel)GetTarget(SessionState.Finished);

            var session = Dummy.CreateSession();

            var cancellationToken = Dummy.CreateCancellationToken();

            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Envelope>(session));

            var actual = await target.ReceiveSessionAsync(cancellationToken).ShouldThrowAsync<InvalidOperationException>();
        }

        #endregion

        #region EnvelopeAsyncBuffer_PromiseAdded

        [Test]
        [Category("EnvelopeAsyncBuffer_PromiseAdded")]

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

            // Assert
            await receiveTask.ShouldThrowAsync<InvalidOperationException>();
            _transport.Verify();
            _transport.Verify(
                t => t.CloseAsync(It.IsAny<CancellationToken>()),
                Times.Once());    
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
        public void Dispose_ReceiveMessageCalled_ThrowsInvalidOperationException()
        {
            // Arrange
            var disposableTransport = _transport.As<IDisposable>();
            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken t) =>
                {
                    t.Register(() => tcs.SetCanceled());
                    return tcs.Task;
                });
            var target = GetTarget(SessionState.Established);
            var receiveMessageTask = target.ReceiveMessageAsync(cancellationToken);

            // Act
            target.Dispose();

            // Assert
            disposableTransport.Verify(
                t => t.Dispose(),
                Times.Once());            
            receiveMessageTask.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        [Category("Dispose")]
        public void Dispose_ReceiveCommandCalled_ThrowsInvalidOperationException()
        {
            // Arrange
            var disposableTransport = _transport.As<IDisposable>();
            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken t) =>
                {
                    t.Register(() => tcs.SetCanceled());
                    return tcs.Task;
                });

            var target = GetTarget(SessionState.Established);
            var receiveCommandTask = target.ReceiveCommandAsync(cancellationToken);

            // Act
            target.Dispose();

            // Assert
            disposableTransport.Verify(
                t => t.Dispose(),
                Times.Once());
            receiveCommandTask.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        [Category("Dispose")]
        public void Dispose_ReceiveNotificationCalled_ThrowsInvalidOperationException()
        {
            // Arrange
            var disposableTransport = _transport.As<IDisposable>();
            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken t) =>
                {
                    t.Register(() => tcs.SetCanceled());
                    return tcs.Task;
                });
            var target = GetTarget(SessionState.Established);
            var receiveNotificationTask = target.ReceiveNotificationAsync(cancellationToken);

            // Act
            target.Dispose();

            // Assert
            disposableTransport.Verify(
                t => t.Dispose(),
                Times.Once());
            receiveNotificationTask.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        [Category("Dispose")]
        public void Dispose_ReceiveSessionCalled_ThrowsInvalidOperationException()
        {
            // Arrange
            var disposableTransport = _transport.As<IDisposable>();
            var cancellationToken = Dummy.CreateCancellationToken();
            var tcs = new TaskCompletionSource<Envelope>();
            _transport
                .Setup(t => t.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken t) =>
                {
                    t.Register(() => tcs.SetCanceled());
                    return tcs.Task;
                });
            var target = GetTarget(SessionState.Established);
            var receiveSessionTask = target.ReceiveSessionAsync(cancellationToken);

            // Act
            target.Dispose();

            // Assert
            disposableTransport.Verify(
                t => t.Dispose(),
                Times.Once());
            receiveSessionTask.ShouldThrow<InvalidOperationException>();
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
            public TestChannel(SessionState state, ITransport transport, TimeSpan sendTimeout, int envelopeBufferSize, bool fillEnvelopeRecipients, Node remoteNode = null, Node localNode = null, bool autoReplyPings = false, TimeSpan? remotePingInterval = null, TimeSpan? remoteIdleTimeout = null, int resendMessageTryCount = 0, TimeSpan? resendMessageInterval = null, TimeSpan? consumeTimeout = null)
                : base(transport, sendTimeout, consumeTimeout ?? sendTimeout, sendTimeout, envelopeBufferSize, fillEnvelopeRecipients, autoReplyPings, remotePingInterval, remoteIdleTimeout, null)
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