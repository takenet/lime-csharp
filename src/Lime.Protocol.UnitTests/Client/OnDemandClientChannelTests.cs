using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    public class OnDemandClientChannelTests
    {
        private TimeSpan _sendTimeout;
        private CancellationToken _cancellationToken;
        private Mock<IClientChannelBuilder> _clientChannelBuilder;
        private Mock<IEstablishedClientChannelBuilder> _establishedClientChannelBuilder;
        private Mock<IClientChannel> _clientChannel;
        private Mock<ITransport> _transport;

        [SetUp]
        public void Setup()
        {
            _sendTimeout = TimeSpan.FromSeconds(5);
            _cancellationToken = _sendTimeout.ToCancellationToken();
            _clientChannel = new Mock<IClientChannel>();
            _transport = new Mock<ITransport>();
            _clientChannel
                .SetupGet(c => c.Transport)
                .Returns(_transport.Object);
            _clientChannelBuilder = new Mock<IClientChannelBuilder>();
            _establishedClientChannelBuilder = new Mock<IEstablishedClientChannelBuilder>();
            _establishedClientChannelBuilder
                .Setup(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_clientChannel.Object);
            _establishedClientChannelBuilder
                .SetupGet(b => b.ChannelBuilder)
                .Returns(_clientChannelBuilder.Object);
            _clientChannelBuilder
                .SetupGet(b => b.SendTimeout)
                .Returns(_sendTimeout);
        }

        [TearDown]
        public void Teardown()
        {
            _clientChannel = null;
            _transport = null;
            _establishedClientChannelBuilder = null;
            _clientChannelBuilder = null;
        }

        private OnDemandClientChannel GetTarget()
        {
            return new OnDemandClientChannel(_establishedClientChannelBuilder.Object);
        }

        [Test]
        public async Task SendMessageAsync_NotEstablishedChannel_BuildChannelAndSends()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();

            // Act
            await target.SendMessageAsync(message);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            _clientChannel.Verify(c => c.SendMessageAsync(message), Times.Once());

        }

        [Test]
        public async Task SendMessageAsync_EstablishedChannel_SendsToExistingChannel()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            await target.SendMessageAsync(message);
            _establishedClientChannelBuilder.ResetCalls();
            _clientChannel.ResetCalls();

            // Act
            await target.SendMessageAsync(message);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            _clientChannel.Verify(c => c.SendMessageAsync(message), Times.Once());
        }

        [Test]
        public async Task SendMessageAsync_ChannelCreationFailed_RecreateChannelAndSend()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException();

            object handlerSender = null;
            ClientChannelExceptionEventArgs handlerArgs = null;
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSender = sender;
                    handlerArgs = args;
                }
            };

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendMessageAsync(message);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.SendMessageAsync(message), Times.Once());
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeFalse();
        }

        [Test]
        public async Task SendMessageAsync_ChannelCreationFailsMultipleTimes_TryRecreateChannelAndSend()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception1 = Dummy.CreateException();
            var exception2 = Dummy.CreateException();
            var exception3 = Dummy.CreateException();
            var handlerSenders = new List<object>();
            var handlerArgs = new List<ClientChannelExceptionEventArgs>();
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSenders.Add(sender);
                    handlerArgs.Add(args);
                }
            };

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception1)
                .Throws(exception2)
                .Throws(exception3)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendMessageAsync(message);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));
            _clientChannel.Verify(c => c.SendMessageAsync(message), Times.Once());
            handlerSenders.Count.ShouldBe(3);
            handlerArgs.Count.ShouldBe(3);
            handlerArgs.Any(h => h.IsConnected).ShouldBeFalse();
            handlerArgs.Select(e => e.Exception).ShouldContain(exception1);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception2);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception3);
            handlerSenders.All(h => h.Equals(target)).ShouldBeTrue();
        }

        [Test]
        [ExpectedException(typeof(ObjectDisposedException))]
        public async Task SendMessageAsync_ChannelDispose_ThrowsObjectDisposed()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            target.Dispose();

            // Act
            await target.SendMessageAsync(message);
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public async Task SendMessageAsync_ChannelCreationFailedAndNotHandled_ThrowsException()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    args.IsHandled = false;
                }
            };
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendMessageAsync(message);
        }

        [Test]
        public async Task SendMessageAsync_ChannelOperationFailed_RecreateChannelAndSend()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException();
            var clientChannel2 = new Mock<IClientChannel>();
            object handlerSender = null;
            ClientChannelExceptionEventArgs handlerArgs = null;
            target.ChannelOperationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSender = sender;
                    handlerArgs = args;
                }
            };
            _clientChannel
                .Setup(c => c.SendMessageAsync(message))
                .Throws(exception);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_clientChannel.Object))
                .Returns(Task.FromResult(clientChannel2.Object));

            // Act
            await target.SendMessageAsync(message);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.SendMessageAsync(message), Times.Once());
            clientChannel2.Verify(c => c.SendMessageAsync(message), Times.Once());
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeFalse();
        }
        
        [Test]
        public async Task SendNotificationAsync_NotEstablishedChannel_BuildChannelAndSends()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            var target = GetTarget();

            // Act
            await target.SendNotificationAsync(notification);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            _clientChannel.Verify(c => c.SendNotificationAsync(notification), Times.Once());
        }

        [Test]
        public async Task SendNotificationAsync_EstablishedChannel_SendsToExistingChannel()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            var target = GetTarget();
            await target.SendNotificationAsync(notification);
            _establishedClientChannelBuilder.ResetCalls();
            _clientChannel.ResetCalls();

            // Act
            await target.SendNotificationAsync(notification);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            _clientChannel.Verify(c => c.SendNotificationAsync(notification), Times.Once());
        }

        [Test]
        public async Task SendNotificationAsync_ChannelCreationFailed_RecreateChannelAndSend()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            var target = GetTarget();
            var exception = Dummy.CreateException();

            object handlerSender = null;
            ClientChannelExceptionEventArgs handlerArgs = null;
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSender = sender;
                    handlerArgs = args;
                }
            };

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendNotificationAsync(notification);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.SendNotificationAsync(notification), Times.Once());
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeFalse();
        }

        [Test]
        public async Task SendNotificationAsync_ChannelCreationFailsMultipleTimes_TryRecreateChannelAndSend()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            var target = GetTarget();
            var exception1 = Dummy.CreateException();
            var exception2 = Dummy.CreateException();
            var exception3 = Dummy.CreateException();
            var handlerSenders = new List<object>();
            var handlerArgs = new List<ClientChannelExceptionEventArgs>();
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSenders.Add(sender);
                    handlerArgs.Add(args);
                }
            };

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception1)
                .Throws(exception2)
                .Throws(exception3)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendNotificationAsync(notification);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));
            _clientChannel.Verify(c => c.SendNotificationAsync(notification), Times.Once());
            handlerSenders.Count.ShouldBe(3);
            handlerArgs.Count.ShouldBe(3);
            handlerArgs.Any(h => h.IsConnected).ShouldBeFalse();
            handlerArgs.Select(e => e.Exception).ShouldContain(exception1);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception2);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception3);
            handlerSenders.All(h => h.Equals(target)).ShouldBeTrue();
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public async Task SendNotificationAsync_ChannelCreationFailedAndNotHandled_ThrowsException()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    args.IsHandled = false;
                }
            };

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendNotificationAsync(notification);
        }

        [Test]
        public async Task SendNotificationAsync_ChannelOperationFailed_RecreateChannelAndSend()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            var target = GetTarget();
            var exception = Dummy.CreateException();
            var clientChannel2 = new Mock<IClientChannel>();
            object handlerSender = null;
            ClientChannelExceptionEventArgs handlerArgs = null;
            target.ChannelOperationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSender = sender;
                    handlerArgs = args;
                }
            };
            _clientChannel
                .SetupSequence(c => c.SendNotificationAsync(notification))
                .Throws(exception)
                .Returns(TaskUtil.CompletedTask);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_clientChannel.Object))
                .Returns(Task.FromResult(clientChannel2.Object));

            // Act
            await target.SendNotificationAsync(notification);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.SendNotificationAsync(notification), Times.Once());
            clientChannel2.Verify(c => c.SendNotificationAsync(notification), Times.Once());
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeFalse();
        }
        
        [Test]
        public async Task SendCommandAsync_NotEstablishedChannel_BuildChannelAndSends()
        {
            // Arrange
            var command = Dummy.CreateCommand();
            var target = GetTarget();

            // Act
            await target.SendCommandAsync(command);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            _clientChannel.Verify(c => c.SendCommandAsync(command), Times.Once());
            
        }

        [Test]
        public async Task SendCommandAsync_EstablishedChannel_SendsToExistingChannel()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            await target.SendCommandAsync(command);
            _establishedClientChannelBuilder.ResetCalls();
            _clientChannel.ResetCalls();

            // Act
            await target.SendCommandAsync(command);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            _clientChannel.Verify(c => c.SendCommandAsync(command), Times.Once());
        }

        [Test]
        public async Task SendCommandAsync_ChannelCreationFailed_RecreateChannelAndSend()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException();

            object handlerSender = null;
            ClientChannelExceptionEventArgs handlerArgs = null;
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSender = sender;
                    handlerArgs = args;
                }
            };

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendCommandAsync(command);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.SendCommandAsync(command), Times.Once());
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeFalse();
        }

        [Test]
        public async Task SendCommandAsync_ChannelCreationFailsMultipleTimes_TryRecreateChannelAndSend()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception1 = Dummy.CreateException();
            var exception2 = Dummy.CreateException();
            var exception3 = Dummy.CreateException();
            var handlerSenders = new List<object>();
            var handlerArgs = new List<ClientChannelExceptionEventArgs>();
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSenders.Add(sender);
                    handlerArgs.Add(args);
                }
            };

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception1)
                .Throws(exception2)
                .Throws(exception3)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendCommandAsync(command);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));
            _clientChannel.Verify(c => c.SendCommandAsync(command), Times.Once());
            handlerSenders.Count.ShouldBe(3);
            handlerArgs.Count.ShouldBe(3);
            handlerArgs.Any(h => h.IsConnected).ShouldBeFalse();
            handlerArgs.Select(e => e.Exception).ShouldContain(exception1);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception2);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception3);
            handlerSenders.All(h => h.Equals(target)).ShouldBeTrue();
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public async Task SendCommandAsync_ChannelCreationFailedAndNotHandled_ThrowsException()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    args.IsHandled = false;
                }
            };

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendCommandAsync(command);
        }

        [Test]
        public async Task SendCommandAsync_ChannelOperationFailed_RecreateChannelAndSend()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException();
            var clientChannel2 = new Mock<IClientChannel>();
            object handlerSender = null;
            ClientChannelExceptionEventArgs handlerArgs = null;
            target.ChannelOperationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSender = sender;
                    handlerArgs = args;
                }
            };
            _clientChannel
                .SetupSequence(c => c.SendCommandAsync(command))
                .Throws(exception)
                .Returns(TaskUtil.CompletedTask);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_clientChannel.Object))
                .Returns(Task.FromResult(clientChannel2.Object));

            // Act
            await target.SendCommandAsync(command);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.SendCommandAsync(command), Times.Once());
            clientChannel2.Verify(c => c.SendCommandAsync(command), Times.Once());
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeFalse();
        }
        
        [Test]
        public async Task ReceiveMessageAsync_NotEstablishedChannel_BuildChannelAndReceives()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            var target = GetTarget();

            // Act
            var actual = await target.ReceiveMessageAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(message);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            _clientChannel.Verify(c => c.ReceiveMessageAsync(_cancellationToken), Times.Once());            
        }

        [Test]
        public async Task ReceiveMessageAsync_EstablishedChannel_ReceivesFromExistingChannel()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            var target = GetTarget();
            await target.ReceiveMessageAsync(_cancellationToken);
            _establishedClientChannelBuilder.ResetCalls();
            _clientChannel.ResetCalls();

            // Act
            var actual = await target.ReceiveMessageAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(message);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            _clientChannel.Verify(c => c.ReceiveMessageAsync(_cancellationToken), Times.Once());
        }

        [Test]
        public async Task ReceiveMessageAsync_ChannelCreationFailed_RecreateChannelAndReceives()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            var target = GetTarget();
            var exception = Dummy.CreateException();

            object handlerSender = null;
            ClientChannelExceptionEventArgs handlerArgs = null;
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSender = sender;
                    handlerArgs = args;
                }
            };

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveMessageAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(message);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.ReceiveMessageAsync(_cancellationToken), Times.Once());
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeFalse();
        }

        [Test]
        public async Task ReceiveMessageAsync_ChannelCreationFailsMultipleTimes_TryRecreateChannelAndReceives()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            var target = GetTarget();
            var exception1 = Dummy.CreateException();
            var exception2 = Dummy.CreateException();
            var exception3 = Dummy.CreateException();
            var handlerSenders = new List<object>();
            var handlerArgs = new List<ClientChannelExceptionEventArgs>();
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSenders.Add(sender);
                    handlerArgs.Add(args);
                }
            };

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception1)
                .Throws(exception2)
                .Throws(exception3)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveMessageAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(message);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));
            _clientChannel.Verify(c => c.ReceiveMessageAsync(_cancellationToken), Times.Once());
            handlerSenders.Count.ShouldBe(3);
            handlerArgs.Count.ShouldBe(3);
            handlerArgs.Any(h => h.IsConnected).ShouldBeFalse();
            handlerArgs.Select(e => e.Exception).ShouldContain(exception1);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception2);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception3);
            handlerSenders.All(h => h.Equals(target)).ShouldBeTrue();
        }

        [Test]
        [ExpectedException(typeof(ObjectDisposedException))]
        public async Task ReceiveMessageAsync_ChannelDispose_ThrowsObjectDisposed()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            target.Dispose();

            // Act
            var actual = await target.ReceiveMessageAsync(_cancellationToken);
        }

        [Test]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task ReceiveMessageAsync_CanceledToken_ThrowsTaskCanceledException()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            var target = GetTarget();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var actual = await target.ReceiveMessageAsync(cts.Token);
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public async Task ReceiveMessageAsync_ChannelCreationFailedAndNotHandled_ThrowsException()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    args.IsHandled = false;
                }
            };
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveMessageAsync(_cancellationToken);
        }

        [Test]
        public async Task ReceiveMessageAsync_ChannelOperationFailed_RecreateChannelAndReceives()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            var target = GetTarget();
            var exception = Dummy.CreateException();
            var clientChannel2 = new Mock<IClientChannel>();
            object handlerSender = null;
            ClientChannelExceptionEventArgs handlerArgs = null;
            target.ChannelOperationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSender = sender;
                    handlerArgs = args;
                }
            };
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Throws(exception);
            clientChannel2
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_clientChannel.Object))
                .Returns(Task.FromResult(clientChannel2.Object));

            // Act
            var actual = await target.ReceiveMessageAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(message);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.ReceiveMessageAsync(_cancellationToken), Times.Once());
            clientChannel2.Verify(c => c.ReceiveMessageAsync(_cancellationToken), Times.Once());
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeFalse();
        }

        [Test]
        public async Task ReceiveNotificationAsync_NotEstablishedChannel_BuildChannelAndReceives()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);
            var target = GetTarget();

            // Act
            var actual = await target.ReceiveNotificationAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(notification);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            _clientChannel.Verify(c => c.ReceiveNotificationAsync(_cancellationToken), Times.Once());
        }

        [Test]
        public async Task ReceiveNotificationAsync_EstablishedChannel_ReceivesFromExistingChannel()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);
            var target = GetTarget();
            await target.ReceiveNotificationAsync(_cancellationToken);
            _establishedClientChannelBuilder.ResetCalls();
            _clientChannel.ResetCalls();

            // Act
            var actual = await target.ReceiveNotificationAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(notification);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            _clientChannel.Verify(c => c.ReceiveNotificationAsync(_cancellationToken), Times.Once());
        }

        [Test]
        public async Task ReceiveNotificationAsync_ChannelCreationFailed_RecreateChannelAndReceives()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);
            var target = GetTarget();
            var exception = Dummy.CreateException();

            object handlerSender = null;
            ClientChannelExceptionEventArgs handlerArgs = null;
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSender = sender;
                    handlerArgs = args;
                }
            };

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveNotificationAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(notification);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.ReceiveNotificationAsync(_cancellationToken), Times.Once());
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeFalse();
        }

        [Test]
        public async Task ReceiveNotificationAsync_ChannelCreationFailsMultipleTimes_TryRecreateChannelAndReceives()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);
            var target = GetTarget();
            var exception1 = Dummy.CreateException();
            var exception2 = Dummy.CreateException();
            var exception3 = Dummy.CreateException();
            var handlerSenders = new List<object>();
            var handlerArgs = new List<ClientChannelExceptionEventArgs>();
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSenders.Add(sender);
                    handlerArgs.Add(args);
                }
            };

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception1)
                .Throws(exception2)
                .Throws(exception3)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveNotificationAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(notification);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));
            _clientChannel.Verify(c => c.ReceiveNotificationAsync(_cancellationToken), Times.Once());
            handlerSenders.Count.ShouldBe(3);
            handlerArgs.Count.ShouldBe(3);
            handlerArgs.Any(h => h.IsConnected).ShouldBeFalse();
            handlerArgs.Select(e => e.Exception).ShouldContain(exception1);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception2);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception3);
            handlerSenders.All(h => h.Equals(target)).ShouldBeTrue();
        }

        [Test]
        [ExpectedException(typeof(ObjectDisposedException))]
        public async Task ReceiveNotificationAsync_ChannelDispose_ThrowsObjectDisposed()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            var target = GetTarget();
            target.Dispose();

            // Act
            var actual = await target.ReceiveNotificationAsync(_cancellationToken);
        }

        [Test]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task ReceiveNotificationAsync_CanceledToken_ThrowsTaskCanceledException()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);
            var target = GetTarget();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var actual = await target.ReceiveNotificationAsync(cts.Token);
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public async Task ReceiveNotificationAsync_ChannelCreationFailedAndNotHandled_ThrowsException()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    args.IsHandled = false;
                }
            };
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveNotificationAsync(_cancellationToken);
        }

        [Test]
        public async Task ReceiveNotificationAsync_ChannelOperationFailed_RecreateChannelAndReceives()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);
            var target = GetTarget();
            var exception = Dummy.CreateException();
            var clientChannel2 = new Mock<IClientChannel>();
            object handlerSender = null;
            ClientChannelExceptionEventArgs handlerArgs = null;
            target.ChannelOperationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSender = sender;
                    handlerArgs = args;
                }
            };
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .Throws(exception);
            clientChannel2
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_clientChannel.Object))
                .Returns(Task.FromResult(clientChannel2.Object));

            // Act
            var actual = await target.ReceiveNotificationAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(notification);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.ReceiveNotificationAsync(_cancellationToken), Times.Once());
            clientChannel2.Verify(c => c.ReceiveNotificationAsync(_cancellationToken), Times.Once());
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeFalse();
        }
        
        [Test]
        public async Task ReceiveCommandAsync_NotEstablishedChannel_BuildChannelAndReceives()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(command);
            var target = GetTarget();

            // Act
            var actual = await target.ReceiveCommandAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(command);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            _clientChannel.Verify(c => c.ReceiveCommandAsync(_cancellationToken), Times.Once());
        }

        [Test]
        public async Task ReceiveCommandAsync_EstablishedChannel_ReceivesFromExistingChannel()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(command);
            var target = GetTarget();
            await target.ReceiveCommandAsync(_cancellationToken);
            _establishedClientChannelBuilder.ResetCalls();
            _clientChannel.ResetCalls();

            // Act
            var actual = await target.ReceiveCommandAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(command);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            _clientChannel.Verify(c => c.ReceiveCommandAsync(_cancellationToken), Times.Once());
        }

        [Test]
        public async Task ReceiveCommandAsync_ChannelCreationFailed_RecreateChannelAndReceives()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(command);
            var target = GetTarget();
            var exception = Dummy.CreateException();

            object handlerSender = null;
            ClientChannelExceptionEventArgs handlerArgs = null;
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSender = sender;
                    handlerArgs = args;
                }
            };

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveCommandAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(command);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.ReceiveCommandAsync(_cancellationToken), Times.Once());
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeFalse();
        }

        [Test]
        public async Task ReceiveCommandAsync_ChannelCreationFailsMultipleTimes_TryRecreateChannelAndReceives()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(command);
            var target = GetTarget();
            var exception1 = Dummy.CreateException();
            var exception2 = Dummy.CreateException();
            var exception3 = Dummy.CreateException();
            var handlerSenders = new List<object>();
            var handlerArgs = new List<ClientChannelExceptionEventArgs>();
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSenders.Add(sender);
                    handlerArgs.Add(args);
                }
            };

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception1)
                .Throws(exception2)
                .Throws(exception3)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveCommandAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(command);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));
            _clientChannel.Verify(c => c.ReceiveCommandAsync(_cancellationToken), Times.Once());
            handlerSenders.Count.ShouldBe(3);
            handlerArgs.Count.ShouldBe(3);
            handlerArgs.Any(h => h.IsConnected).ShouldBeFalse();
            handlerArgs.Select(e => e.Exception).ShouldContain(exception1);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception2);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception3);
            handlerSenders.All(h => h.Equals(target)).ShouldBeTrue();
        }

        [Test]
        [ExpectedException(typeof(ObjectDisposedException))]
        public async Task ReceiveCommandAsync_ChannelDispose_ThrowsObjectDisposed()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            target.Dispose();

            // Act
            var actual = await target.ReceiveCommandAsync(_cancellationToken);
        }

        [Test]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task ReceiveCommandAsync_CanceledToken_ThrowsTaskCanceledException()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(command);
            var target = GetTarget();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var actual = await target.ReceiveCommandAsync(cts.Token);
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public async Task ReceiveCommandAsync_ChannelCreationFailedAndNotHandled_ThrowsException()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    args.IsHandled = false;
                }
            };
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveCommandAsync(_cancellationToken);
        }

        [Test]
        public async Task ReceiveCommandAsync_ChannelOperationFailed_RecreateChannelAndReceives()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(command);
            var target = GetTarget();
            var exception = Dummy.CreateException();
            var clientChannel2 = new Mock<IClientChannel>();
            object handlerSender = null;
            ClientChannelExceptionEventArgs handlerArgs = null;
            target.ChannelOperationFailed += (sender, args) =>
            {
                using (args.GetDeferral())
                {
                    handlerSender = sender;
                    handlerArgs = args;
                }
            };
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .Throws(exception);
            clientChannel2
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(command);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_clientChannel.Object))
                .Returns(Task.FromResult(clientChannel2.Object));

            // Act
            var actual = await target.ReceiveCommandAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(command);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.ReceiveCommandAsync(_cancellationToken), Times.Once());
            clientChannel2.Verify(c => c.ReceiveCommandAsync(_cancellationToken), Times.Once());
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeFalse();
        }

    }
}