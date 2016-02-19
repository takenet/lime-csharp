using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Network.Modules;
using Moq;
using NUnit.Core;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Network.Modules
{
    [TestFixture]
    public class ResendMessagesChannelModuleTests
    {
        private Mock<ITransport> _transport;
        private Mock<IChannel> _channel;
        private int _resendMessageTryCount;
        private TimeSpan _resendMessageInterval;
        private TimeSpan _resendMessageIntervalWithSafeMargin;
        private bool _filterByDestination;
        private CancellationToken _cancellationToken;
        

        #region Scenario

        [SetUp]
        public void Setup()
        {
            _transport = new Mock<ITransport>();
            _transport.Setup(t => t.IsConnected).Returns(true);
            _channel = CreateChannel();
            _resendMessageTryCount = 3;
            _resendMessageInterval = TimeSpan.FromMilliseconds(200);
            _resendMessageIntervalWithSafeMargin = TimeSpan.FromMilliseconds(300);
            _cancellationToken = CancellationToken.None;
            _filterByDestination = false;
        }


        [TearDown]
        public void Teardown()
        {
            _channel = null;
            _transport = null;
        }

        #endregion

        private Mock<IChannel> CreateChannel()
        {
            var channel = new Mock<IChannel>();
            channel.Setup(c => c.Transport).Returns(_transport.Object);
            channel.Setup(c => c.MessageModules).Returns(() => new List<IChannelModule<Message>>());
            channel.Setup(c => c.NotificationModules).Returns(() => new List<IChannelModule<Notification>>());
            channel.Setup(c => c.State).Returns(SessionState.Established);
            return channel;
        }

        private ResendMessagesChannelModule GetTarget(bool bindToChannel = true)
        {
            var target = new ResendMessagesChannelModule(_resendMessageTryCount, _resendMessageInterval, _filterByDestination);            
            if (bindToChannel)
            {
                target.Bind(_channel.Object, true);
                _channel
                    .Setup(c => c.SendMessageAsync(It.IsAny<Message>()))
                    .Returns((Message m) => ((IChannelModule<Message>) target).OnSendingAsync(m, _cancellationToken));
            }
            return target;
        }

        [Test]
        public async Task OnSending_MessageWithoutNotification_ShouldResendAfterInterval()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = Guid.NewGuid();
            _resendMessageTryCount = 1;
            var target = GetTarget();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Exactly(1));            
        }

        [Test]
        public async Task OnSending_MultipleMessagesWithoutNotification_ShouldResendAfterInterval()
        {
            // Arrange
            var messages = new List<Message>();
            for (int i = 0; i < Dummy.CreateRandomInt(100) + 1; i++)
            {
                var message = Dummy.CreateMessage(Dummy.CreateTextContent());
                message.Id = Guid.NewGuid();
                messages.Add(message);
            }
            _resendMessageTryCount = 1;
            var target = GetTarget();

            // Act
            var actuals = new List<Message>();
            foreach (var message in messages)
            {
                var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
                actuals.Add(actual);
            }            
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            foreach (var message in messages)
            {
                actuals.ShouldContain(message);
                _channel.Verify(c => c.SendMessageAsync(message), Times.Exactly(1));
            }                            
        }

        [Test]
        public async Task OnSending_MessageWithoutNotification_ShouldResendUntilLimit()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = Guid.NewGuid();
            var target = GetTarget();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            await Task.Delay(TimeSpan.FromTicks(_resendMessageIntervalWithSafeMargin.Ticks * (_resendMessageTryCount + 1)));

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Exactly(_resendMessageTryCount));
        }


        [Test]
        public async Task OnSending_MultipleMessagesWithoutNotification_ShouldResendUntilLimit()
        {
            // Arrange
            var messages = new List<Message>();
            for (int i = 0; i < Dummy.CreateRandomInt(100) + 1; i++)
            {
                var message = Dummy.CreateMessage(Dummy.CreateTextContent());
                message.Id = Guid.NewGuid();
                messages.Add(message);
            }
            var target = GetTarget();

            // Act
            var actuals = new List<Message>();
            foreach (var message in messages)
            {
                var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
                actuals.Add(actual);
            }
            await Task.Delay(TimeSpan.FromTicks(_resendMessageIntervalWithSafeMargin.Ticks * (_resendMessageTryCount + 1)));

            // Assert            
            foreach (var message in messages)
            {
                actuals.ShouldContain(message);
                _channel.Verify(c => c.SendMessageAsync(message), Times.Exactly(_resendMessageTryCount));
            }
        }

        [Test]
        public async Task OnSending_ReceivedNotificationAfterSend_ShouldNotResend()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = Guid.NewGuid();
            var notification = Dummy.CreateNotification(Event.Received);
            notification.Id = message.Id;
            notification.From = message.To;
            var target = GetTarget();

            // Act
            var actualMessage = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            var actualNotification = await ((IChannelModule<Notification>)target).OnReceivingAsync(notification, _cancellationToken);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actualMessage.ShouldBe(message);
            actualNotification.ShouldBe(notification);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Never);
        }

        [Test]
        public async Task OnSending_ReceivedNotificationAfterFirstResend_ShouldNotResendAgain()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = Guid.NewGuid();
            var notification = Dummy.CreateNotification(Event.Received);
            notification.Id = message.Id;
            notification.From = message.To;            
            var target = GetTarget();

            // Act
            var actualMessage = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);
            var actualNotification = await ((IChannelModule<Notification>)target).OnReceivingAsync(notification, _cancellationToken);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actualMessage.ShouldBe(message);
            actualNotification.ShouldBe(notification);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Exactly(1));
        }

        [Test]
        public async Task OnSending_ReceivedNotificationAFromDifferentSenderWhenFilteringByDestination_ShouldResendUntilLimit()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = Guid.NewGuid();
            var notification = Dummy.CreateNotification(Event.Received);
            notification.Id = message.Id;
            notification.From = Dummy.CreateNode(); // Other sender
            _filterByDestination = true;
            var target = GetTarget();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);            
            var actualNotification = await ((IChannelModule<Notification>)target).OnReceivingAsync(notification, _cancellationToken);
            await Task.Delay(TimeSpan.FromTicks(_resendMessageIntervalWithSafeMargin.Ticks * (_resendMessageTryCount + 1)));

            // Assert
            actual.ShouldBe(message);
            actualNotification.ShouldBe(notification);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Exactly(_resendMessageTryCount));
        }


        [Test]
        public async Task OnSending_ReceivedNotificationAfterSecondResend_ShouldNotResendAgain()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = Guid.NewGuid();
            var notification = Dummy.CreateNotification(Event.Received);
            notification.Id = message.Id;
            notification.From = message.To;
            var target = GetTarget();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            await Task.Delay(TimeSpan.FromTicks(_resendMessageIntervalWithSafeMargin.Ticks * 2));
            var actualNotification = await ((IChannelModule<Notification>)target).OnReceivingAsync(notification, _cancellationToken);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            actualNotification.ShouldBe(notification);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Exactly(2));
        }

        [Test]
        public async Task OnStateChanged_EstablishedToFinished_ShouldNotResend()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = Guid.NewGuid();
            var target = GetTarget();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            target.OnStateChanged(SessionState.Finished);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Never);
        }


        [Test]
        public async Task OnStateChanged_EstablishedToFinishedAfterSecondResend_ShouldNotResendAgain()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = Guid.NewGuid();           
            var target = GetTarget();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            await Task.Delay(TimeSpan.FromTicks(_resendMessageIntervalWithSafeMargin.Ticks * 2));
            target.OnStateChanged(SessionState.Finished);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Exactly(2));
        }

        [Test]
        public async Task OnStateChanged_EstablishedToFailed_ShouldNotResend()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = Guid.NewGuid();
            var target = GetTarget();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            target.OnStateChanged(SessionState.Failed);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Never);
        }

        [Test]
        public async Task OnStateChanged_EstablishedToFailedAfterSecondResend_ShouldNotResendAgain()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = Guid.NewGuid();
            var target = GetTarget();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            await Task.Delay(TimeSpan.FromTicks(_resendMessageIntervalWithSafeMargin.Ticks * 2));
            target.OnStateChanged(SessionState.Failed);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Exactly(2));
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Bind_AlreadyBound_ThrowInvalidOperationException()
        {
            // Arrange
            var target = GetTarget();
            var channel2Mock = CreateChannel();

            // Act
            target.Bind(channel2Mock.Object, true);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Unbind_NotBound_ThrowInvalidOperationException()
        {
            // Arrange
            var target = GetTarget(false);

            // Act
            target.Unbind();
        }

        [Test]
        public async Task Unbind_PendingMessage_ShouldNotResend()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = Guid.NewGuid();
            var target = GetTarget();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            target.Unbind();
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Never);
        }


        [Test]
        public async Task Unbind_MultiplePendingMessages_ShouldNotResend()
        {
            // Arrange
            var messages = new List<Message>();
            for (int i = 0; i < Dummy.CreateRandomInt(100) + 1; i++)
            {
                var message = Dummy.CreateMessage(Dummy.CreateTextContent());
                message.Id = Guid.NewGuid();
                messages.Add(message);
            }
            _resendMessageTryCount = 1;
            var target = GetTarget();

            // Act
            var actuals = new List<Message>();
            foreach (var message in messages)
            {
                var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
                actuals.Add(actual);
            }
            target.Unbind();
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            foreach (var message in messages)
            {
                actuals.ShouldContain(message);
                _channel.Verify(c => c.SendMessageAsync(message), Times.Never);
            }
        }

        [Test]
        public async Task Unbind_PendingMessageAndBoundToNewChannel_SendsToBoundChannel()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = Guid.NewGuid();
            var target = GetTarget();
            var channel2Mock = CreateChannel();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            target.Unbind();
            target.Bind(channel2Mock.Object, true);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Never);
            channel2Mock.Verify(c => c.SendMessageAsync(message), Times.Once);
        }

        [Test]
        public async Task Unbind_MultiplePendingMessagesAndBoundToNewChannel_SendsToBoundChannel()
        {
            // Arrange
            var messages = new List<Message>();
            for (int i = 0; i < Dummy.CreateRandomInt(100) + 1; i++)
            {
                var message = Dummy.CreateMessage(Dummy.CreateTextContent());
                message.Id = Guid.NewGuid();
                messages.Add(message);
            }
            var target = GetTarget();
            var channel2Mock = CreateChannel();

            // Act
            var actuals = new List<Message>();
            foreach (var message in messages)
            {
                var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
                actuals.Add(actual);
            }
            target.Unbind();
            target.Bind(channel2Mock.Object, true);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);
            
            // Assert
            foreach (var message in messages)
            {
                actuals.ShouldContain(message);
                _channel.Verify(c => c.SendMessageAsync(message), Times.Never);
                channel2Mock.Verify(c => c.SendMessageAsync(message), Times.Once);
            }            
        }
    }
}
