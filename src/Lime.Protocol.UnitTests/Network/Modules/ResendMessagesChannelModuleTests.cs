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
        private List<IChannelModule<Message>> _messageModules;
        private List<IChannelModule<Notification>> _notificationModules;
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
            _messageModules = new List<IChannelModule<Message>>();
            _notificationModules = new List<IChannelModule<Notification>>();
            _channel = new Mock<IChannel>();
            _channel.Setup(c => c.Transport).Returns(_transport.Object);
            _channel.Setup(c => c.MessageModules).Returns(() => _messageModules);
            _channel.Setup(c => c.NotificationModules).Returns(() => _notificationModules);
            _channel.Setup(c => c.State).Returns(SessionState.Established);
            _resendMessageTryCount = 3;
            _resendMessageInterval = TimeSpan.FromMilliseconds(200);
            _resendMessageIntervalWithSafeMargin = TimeSpan.FromMilliseconds(250);
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

        public ResendMessagesChannelModule GetTarget()
        {
            return ResendMessagesChannelModule.CreateAndRegister(_channel.Object, _resendMessageTryCount, _resendMessageInterval, _filterByDestination);
        }


        [Test]
        public async Task OnSending_MessageWithoutNotification_ShouldResendAfterInterval()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = Guid.NewGuid();
            var target = GetTarget();
            target.OnStateChanged(SessionState.Established);

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSending(message, _cancellationToken);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Exactly(1));            
        }

        [Test]
        public async Task OnSending_MessageWithoutNotification_ShouldResendUntilLimit()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = Guid.NewGuid();
            var target = GetTarget();
            target.OnStateChanged(SessionState.Established);

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSending(message, _cancellationToken);
            await Task.Delay(TimeSpan.FromTicks(_resendMessageIntervalWithSafeMargin.Ticks * (_resendMessageTryCount + 1)));

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Exactly(_resendMessageTryCount));
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
            target.OnStateChanged(SessionState.Established);

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSending(message, _cancellationToken);
            var actualNotification = await ((IChannelModule<Notification>)target).OnReceiving(notification, _cancellationToken);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
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
            target.OnStateChanged(SessionState.Established);

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSending(message, _cancellationToken);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);
            var actualNotification = await ((IChannelModule<Notification>)target).OnReceiving(notification, _cancellationToken);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
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
            target.OnStateChanged(SessionState.Established);

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSending(message, _cancellationToken);            
            var actualNotification = await ((IChannelModule<Notification>)target).OnReceiving(notification, _cancellationToken);
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
            target.OnStateChanged(SessionState.Established);

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSending(message, _cancellationToken);
            await Task.Delay(TimeSpan.FromTicks(_resendMessageIntervalWithSafeMargin.Ticks * 2));
            var actualNotification = await ((IChannelModule<Notification>)target).OnReceiving(notification, _cancellationToken);
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
            target.OnStateChanged(SessionState.Established);

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSending(message, _cancellationToken);
            target.OnStateChanged(SessionState.Finished);            

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
            target.OnStateChanged(SessionState.Established);

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSending(message, _cancellationToken);
            await Task.Delay(TimeSpan.FromTicks(_resendMessageIntervalWithSafeMargin.Ticks * 2));
            target.OnStateChanged(SessionState.Finished);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message), Times.Exactly(2));
        }
    }
}
