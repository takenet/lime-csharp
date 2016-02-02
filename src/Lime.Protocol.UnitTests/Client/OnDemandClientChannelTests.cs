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
                .SetupSequence(c => c.SendMessageAsync(message))
                .Throws(exception)
                .Returns(TaskUtil.CompletedTask);
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
        public async Task SendMessageAsync_ChannelOperationFailedButStillValid_CallsSendAgain()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException();            
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
                .SetupGet(c => c.State)
                .Returns(SessionState.Established);
            _transport
                .SetupGet(t => t.IsConnected)
                .Returns(true);
            _clientChannel
                .SetupSequence(c => c.SendMessageAsync(message))
                .Throws(exception)
                .Returns(TaskUtil.CompletedTask);

            // Act
            await target.SendMessageAsync(message);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(1));
            _clientChannel.Verify(c => c.SendMessageAsync(message), Times.Exactly(2));            
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeTrue();
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
        public async Task SendNotificationAsync_ChannelOperationFailedButStillValid_CallsSendAgain()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Consumed);
            var target = GetTarget();
            var exception = Dummy.CreateException();
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
                .SetupGet(c => c.State)
                .Returns(SessionState.Established);
            _transport
                .SetupGet(t => t.IsConnected)
                .Returns(true);
            _clientChannel
                .SetupSequence(c => c.SendNotificationAsync(notification))
                .Throws(exception)
                .Returns(TaskUtil.CompletedTask);

            // Act
            await target.SendNotificationAsync(notification);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(1));
            _clientChannel.Verify(c => c.SendNotificationAsync(notification), Times.Exactly(2));
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeTrue();
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
        public async Task SendCommandAsync_ChannelOperationFailedButStillValid_CallsSendAgain()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException();
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
                .SetupGet(c => c.State)
                .Returns(SessionState.Established);
            _transport
                .SetupGet(t => t.IsConnected)
                .Returns(true);
            _clientChannel
                .SetupSequence(c => c.SendCommandAsync(command))
                .Throws(exception)
                .Returns(TaskUtil.CompletedTask);

            // Act
            await target.SendCommandAsync(command);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(1));
            _clientChannel.Verify(c => c.SendCommandAsync(command), Times.Exactly(2));
            handlerSender.ShouldNotBeNull();
            handlerSender.ShouldBe(target);
            handlerArgs.Exception.ShouldBe(exception);
            handlerArgs.IsConnected.ShouldBeTrue();
        }

    }
}