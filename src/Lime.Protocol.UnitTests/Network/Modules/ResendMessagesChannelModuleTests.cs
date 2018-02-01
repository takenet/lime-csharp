using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Network.Modules;
using Lime.Protocol.Network.Modules.Resend;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Lime.Protocol.UnitTests.Network.Modules
{
    
    public class ResendMessagesChannelModuleTests : IDisposable
    {
        private readonly Mock<ITransport> _transport;
        private readonly Mock<IChannel> _channel;
        private int _resendMessageTryCount;
        private readonly TimeSpan _resendMessageInterval;
        private TimeSpan _resendMessageIntervalWithSafeMargin;
        private bool _filterByDestination;
        private readonly CancellationToken _cancellationToken;
        private readonly List<ResendMessagesChannelModule> _targets;

        public ResendMessagesChannelModuleTests()
        {
            _transport = new Mock<ITransport>();
            _transport.Setup(t => t.IsConnected).Returns(true);
            _channel = CreateChannel();
            _resendMessageTryCount = 3;
            _resendMessageInterval = TimeSpan.FromMilliseconds(500);
            _resendMessageIntervalWithSafeMargin = TimeSpan.FromMilliseconds(550);
            _cancellationToken = CancellationToken.None;
            _filterByDestination = false;
            _targets = new List<ResendMessagesChannelModule>();
        }

        private TimeSpan ResendLimit => TimeSpan.FromTicks(
            (Enumerable.Range(1, _resendMessageTryCount + 1).Sum() + _resendMessageTryCount) * _resendMessageIntervalWithSafeMargin.Ticks);

        private Mock<IChannel> CreateChannel(Node localNode = null, Node remoteNode = null)
        {
            var channel = new Mock<IChannel>();
            channel.Setup(c => c.Transport).Returns(_transport.Object);
            channel.Setup(c => c.MessageModules).Returns(() => new List<IChannelModule<Message>>());
            channel.Setup(c => c.NotificationModules).Returns(() => new List<IChannelModule<Notification>>());
            channel.Setup(c => c.State).Returns(SessionState.Established);
            channel.Setup(c => c.LocalNode).Returns(localNode ?? Dummy.CreateNode());
            channel.Setup(c => c.RemoteNode).Returns(remoteNode ?? Dummy.CreateNode());

            return channel;
        }

        private ResendMessagesChannelModule GetTarget(
            bool setEstablishedState = true, 
            Mock<IChannel> channel = null, 
            IMessageStorage messageStorage = null, 
            IDeadMessageHandler deadMessageHandler = null,
            Event[] eventsToRemovePendingMessage = null)
        {
            if (channel == null) channel = _channel;

            var target =
                ResendMessagesChannelModule.CreateAndRegister(channel.Object, _resendMessageTryCount,
                    _resendMessageInterval, messageStorage, deadMessageHandler: deadMessageHandler, eventsToRemovePendingMessage: eventsToRemovePendingMessage);

            channel
                .Setup(c => c.SendMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
                .Returns((Message m, CancellationToken c) => ((IChannelModule<Message>)target).OnSendingAsync(m, _cancellationToken));

            if (setEstablishedState) target.OnStateChanged(SessionState.Established);
            
            _targets.Add(target);
            return target;
        }

        [TestMethod]
        public async Task OnSending_MessageWithoutNotification_ShouldResendAfterInterval()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = EnvelopeId.NewId();
            _resendMessageTryCount = 1;
            var target = GetTarget();            

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Exactly(1));            
        }

        [TestMethod]
        public async Task OnSending_MultipleMessagesWithoutNotification_ShouldResendAfterInterval()
        {
            // Arrange
            var messages = new List<Message>();
            for (int i = 0; i < Dummy.CreateRandomInt(100) + 1; i++)
            {
                var message = Dummy.CreateMessage(Dummy.CreateTextContent());
                message.Id = EnvelopeId.NewId();
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
            await Task.Delay(ResendLimit);

            // Assert
            foreach (var message in messages)
            {
                actuals.ShouldContain(message);
                _channel.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Exactly(1));
            }
        }

        [TestMethod]
        public async Task OnSending_MessageWithoutNotification_ShouldResendUntilLimit()
        {
            // Arrange
            var deadMessageHandler = new Mock<IDeadMessageHandler>();
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = EnvelopeId.NewId();
            var target = GetTarget(deadMessageHandler: deadMessageHandler.Object);

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            await Task.Delay(ResendLimit);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Exactly(_resendMessageTryCount));
            deadMessageHandler.Verify(h => h.HandleDeadMessageAsync(message, It.IsAny<IChannelInformation>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [TestMethod]
        public async Task OnSending_MessageWithNotificationFromDifferentEvent_ShouldResendUntilLimit()
        {
            // Arrange
            var deadMessageHandler = new Mock<IDeadMessageHandler>();
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = EnvelopeId.NewId();
            var notification = Dummy.CreateNotification(Event.Received);
            notification.Id = message.Id;
            notification.From = message.To;            
            var target = GetTarget(deadMessageHandler: deadMessageHandler.Object, eventsToRemovePendingMessage: new[] { Event.Accepted });

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            await ((IChannelModule<Notification>)target).OnReceivingAsync(notification, _cancellationToken);
            await Task.Delay(ResendLimit);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Exactly(_resendMessageTryCount));
            deadMessageHandler.Verify(h => h.HandleDeadMessageAsync(message, It.IsAny<IChannelInformation>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [TestMethod]
        public async Task OnSending_MultipleMessagesWithoutNotification_ShouldResendUntilLimit()
        {
            // Arrange
            var messages = new List<Message>();
            for (int i = 0; i < Dummy.CreateRandomInt(100) + 1; i++)
            {
                var message = Dummy.CreateMessage(Dummy.CreateTextContent());
                message.Id = EnvelopeId.NewId();
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
            await Task.Delay(ResendLimit);

            // Assert            
            foreach (var message in messages)
            {
                actuals.ShouldContain(message);
                _channel.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Exactly(_resendMessageTryCount));
            }
        }

        [TestMethod]
        public async Task OnSending_ReceivedNotificationAfterSend_ShouldNotResend()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = EnvelopeId.NewId();
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
            _channel.Verify(c => c.SendMessageAsync(message, CancellationToken.None), Times.Never);
        }

        [TestMethod]
        public async Task OnSending_ReceivedNotificationAfterFirstResend_ShouldNotResendAgain()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = EnvelopeId.NewId();
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
            _channel.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [TestMethod]
        public async Task OnSending_ReceivedNotificationAFromDifferentSenderWhenFilteringByDestination_ShouldResendUntilLimit()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = EnvelopeId.NewId();
            var notification = Dummy.CreateNotification(Event.Received);
            notification.Id = message.Id;
            notification.From = Dummy.CreateNode(); // Other sender
            _filterByDestination = true;
            var target = GetTarget();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);            
            var actualNotification = await ((IChannelModule<Notification>)target).OnReceivingAsync(notification, _cancellationToken);
            await Task.Delay(ResendLimit);

            // Assert
            actual.ShouldBe(message);
            actualNotification.ShouldBe(notification);
            _channel.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Exactly(_resendMessageTryCount));
        }


        [TestMethod]
        public async Task OnSending_ReceivedNotificationAfterSecondResend_ShouldNotResendAgain()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = EnvelopeId.NewId();
            var notification = Dummy.CreateNotification(Event.Received);
            notification.Id = message.Id;
            notification.From = message.To;
            var channel = CreateChannel();
            var target = GetTarget(channel: channel);
            var callCount = 0;
            channel
                .Setup(c => c.SendMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
                .Returns((Message m, CancellationToken c) => ((IChannelModule<Message>)target).OnSendingAsync(m, _cancellationToken))
                .Callback(async (Message m,  CancellationToken c) =>
                {
                    callCount++;
                    if (callCount == 2)
                    {
                        await ((IChannelModule<Notification>)target).OnReceivingAsync(notification, _cancellationToken);
                    }
                });


            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            await Task.Delay(ResendLimit);

            // Assert
            actual.ShouldBe(message);
            channel.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task OnStateChanged_EstablishedToFinished_ShouldNotResend()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = EnvelopeId.NewId();
            var target = GetTarget();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            target.OnStateChanged(SessionState.Finished);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message, CancellationToken.None), Times.Never);
        }


        [TestMethod]
        public async Task OnStateChanged_EstablishedToFinishedAfterSecondResend_ShouldNotResendAgain()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = EnvelopeId.NewId();           
            var target = GetTarget();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            await Task.Delay(TimeSpan.FromTicks(_resendMessageIntervalWithSafeMargin.Ticks * 3));
            target.OnStateChanged(SessionState.Finished);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task OnStateChanged_EstablishedToFailed_ShouldNotResend()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = EnvelopeId.NewId();
            var target = GetTarget();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            target.OnStateChanged(SessionState.Failed);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task OnStateChanged_EstablishedToFailedAfterSecondResend_ShouldNotResendAgain()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = EnvelopeId.NewId();
            var target = GetTarget();

            // Act
            var actual = await ((IChannelModule<Message>)target).OnSendingAsync(message, _cancellationToken);
            await Task.Delay(TimeSpan.FromTicks(_resendMessageIntervalWithSafeMargin.Ticks * 4));
            target.OnStateChanged(SessionState.Failed);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task OnStateChanged_PendingMessageAndRegisteredToNewChannel_SendsToNewChannel()
        {
            // Arrange
            var messageStorage = new MemoryMessageStorage();
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            message.Id = EnvelopeId.NewId();
            var target1 = GetTarget(messageStorage: messageStorage);
            var channel2Mock = CreateChannel(_channel.Object.LocalNode, _channel.Object.RemoteNode);

            // Act
            var actual = await ((IChannelModule<Message>)target1).OnSendingAsync(message, _cancellationToken);
            target1.OnStateChanged(SessionState.Finished);
            var target2 = GetTarget(messageStorage: messageStorage, channel: channel2Mock);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            actual.ShouldBe(message);
            _channel.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Never);
            channel2Mock.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task OnStateChanged_MultiplePendingMessagesAndRegisteredToNewChannel_SendsToNewChannel()
        {
            // Arrange
            var messageStorage = new MemoryMessageStorage();
            var messages = new List<Message>();
            for (int i = 0; i < Dummy.CreateRandomInt(100) + 1; i++)
            {
                var message = Dummy.CreateMessage(Dummy.CreateTextContent());
                message.Id = EnvelopeId.NewId();
                messages.Add(message);
            }
            var target1 = GetTarget(messageStorage: messageStorage);
            var channel2Mock = CreateChannel(_channel.Object.LocalNode, _channel.Object.RemoteNode);

            // Act
            var actuals = new List<Message>();
            foreach (var message in messages)
            {
                var actual = await ((IChannelModule<Message>)target1).OnSendingAsync(message, _cancellationToken);
                actuals.Add(actual);
            }
            target1.OnStateChanged(SessionState.Finished);
            var target2 = GetTarget(messageStorage: messageStorage, channel: channel2Mock);
            await Task.Delay(_resendMessageIntervalWithSafeMargin);

            // Assert
            foreach (var message in messages)
            {
                actuals.ShouldContain(message);
                _channel.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Never);
                channel2Mock.Verify(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        public void Dispose()
        {
            _targets.ForEach(t => t.Dispose());
        }
    }
}
