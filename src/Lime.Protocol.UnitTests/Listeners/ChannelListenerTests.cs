using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;
using Lime.Protocol.Util;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Lime.Protocol.UnitTests.Listeners
{
    [TestClass]
    public class ChannelListenerTests : IDisposable
    {
        protected Mock<IEstablishedReceiverChannel> _channel;
        protected BlockingCollection<Message> _producedMessages;
        protected BlockingCollection<Notification> _producedNotifications;
        protected BlockingCollection<Command> _producedCommands;
        protected BlockingCollection<Message> _consumedMessages;
        protected BlockingCollection<Notification> _consumedNotifications;
        protected BlockingCollection<Command> _consumedCommands;
        protected Func<Message, Task<bool>> _messageConsumer;
        protected Func<Notification, Task<bool>> _notificationConsumer;
        protected Func<Command, Task<bool>> _commandConsumer;

        private CancellationToken _cancellationToken;
        private Message _completionMessage;
        private Notification _completionNotification;
        private Command _completionCommand;
        private List<ChannelListener> _createdTargets;

        public ChannelListenerTests()
        {
            _channel = new Mock<IEstablishedReceiverChannel>();
            
            _producedMessages = new BlockingCollection<Message>();
            _producedNotifications = new BlockingCollection<Notification>();
            _producedCommands = new BlockingCollection<Command>();

            _channel
                .Setup(m => m.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) => _producedMessages.Take(cancellationToken).AsCompletedTask());
            _channel
                .Setup(m => m.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) => _producedNotifications.Take(cancellationToken).AsCompletedTask());
            _channel
                .Setup(m => m.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) => _producedCommands.Take(cancellationToken).AsCompletedTask());

            _consumedMessages = new BlockingCollection<Message>();
            _consumedNotifications = new BlockingCollection<Notification>();
            _consumedCommands = new BlockingCollection<Command>();

            _cancellationToken = TimeSpan.FromSeconds(15).ToCancellationToken();

            _messageConsumer = m =>
            {                
                if (ReferenceEquals(m, _completionMessage))
                {
                    _consumedMessages.CompleteAdding();
                    return TaskUtil.FalseCompletedTask;
                }
                _consumedMessages.Add(m, _cancellationToken);
                return TaskUtil.TrueCompletedTask;
            };
            _notificationConsumer = n =>
            {                
                if (ReferenceEquals(n, _completionNotification))
                {
                    _consumedNotifications.CompleteAdding();
                    return TaskUtil.FalseCompletedTask;
                }
                _consumedNotifications.Add(n, _cancellationToken);
                return TaskUtil.TrueCompletedTask;
            };
            _commandConsumer = c =>
            {                
                if (ReferenceEquals(c, _completionCommand))
                {
                    _consumedCommands.CompleteAdding();
                    return TaskUtil.FalseCompletedTask;
                }
                _consumedCommands.Add(c, _cancellationToken);
                return TaskUtil.TrueCompletedTask;
            };

            _completionMessage = Dummy.CreateMessage(Dummy.CreateTextContent());
            _completionNotification = Dummy.CreateNotification(Event.Consumed);
            _completionCommand = Dummy.CreateCommand();
        }

        public void Dispose()
        {
            _channel = null;
            _channel = null;
            _channel = null;
            _producedMessages = null;
            _producedNotifications = null;
            _producedCommands = null;
            _consumedMessages = null;
            _consumedNotifications = null;
            _consumedCommands = null;
            _cancellationToken = CancellationToken.None;

            if (_createdTargets != null)
            {
                foreach (var target in _createdTargets)
                {
                    target.Dispose();
                }
            }
        }


        protected ChannelListener GetAndStartTarget()
        {
            if (_createdTargets == null)
            {
                _createdTargets = new List<ChannelListener>();
            }

            var target = new ChannelListener(_messageConsumer, _notificationConsumer, _commandConsumer);
            target.Start(_channel.Object);
            _createdTargets.Add(target);
            return target;
        }

        [TestMethod]
        public async Task Start_MessageReceived_CallsConsumer()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            var target = GetAndStartTarget();

            // Act
            _producedMessages.Add(message);
            var actual = _consumedMessages.Take(_cancellationToken);

            // Assert            
            actual.ShouldBe(message);            
            _producedMessages.Add(_completionMessage);            
            (await target.MessageListenerTask.WithCancellation(_cancellationToken)).ShouldBe(_completionMessage);
            _channel.Verify(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task Start_MultipleMessagesReceived_CallsConsumer()
        {
            // Arrange
            var messages = new List<Message>();
            var count = Dummy.CreateRandomInt(100) + 2;
            for (int i = 0; i < count; i++)
            {
                messages.Add(
                    Dummy.CreateMessage(Dummy.CreateTextContent()));
            }            
            var target = GetAndStartTarget();            

            // Act
            foreach (var message in messages)
            {
                _producedMessages.Add(message);
            }

            // Assert
            _producedMessages.Add(_completionMessage);
            (await target.MessageListenerTask.WithCancellation(_cancellationToken)).ShouldBe(_completionMessage);            
            _consumedMessages.Count.ShouldBe(count);
            _channel.Verify(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Exactly(count + 1));                        
        }

        [TestMethod]
        public async Task Start_ConsumerCompletedWhileProducingMessages_StopsConsuming()
        {
            // Arrange
            var messages = new List<Message>();
            var count = Dummy.CreateRandomInt(500) + 2;
            var halfCount = count/2;
            for (int i = 0; i < count; i++)
            {
                messages.Add(
                    Dummy.CreateMessage(Dummy.CreateTextContent()));
            }

            int consumedCount = 0;
            _messageConsumer = (m) =>
            {
                consumedCount++;
                if (consumedCount == halfCount)
                {
                    return TaskUtil.FalseCompletedTask;
                }
                return TaskUtil.TrueCompletedTask;
            };

            var target = GetAndStartTarget();

            // Act
            foreach (var message in messages)
            {
                _producedMessages.Add(message);
            }

            // Assert
            var unconsumedMessage = await target.MessageListenerTask;
            unconsumedMessage.ShouldNotBeNull();
            _producedMessages.ShouldNotContain(unconsumedMessage);
            _consumedMessages.ShouldNotContain(unconsumedMessage);
            consumedCount.ShouldBe(halfCount);
            _channel.Verify(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Exactly(halfCount));
            _producedMessages.Count.ShouldBe(count - halfCount);                                 
        }

        [TestMethod]        
        public async Task Start_MessageChannelThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var exception = Dummy.CreateException<ApplicationException>();            
            _channel
                .Setup(m => m.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            var target = GetAndStartTarget();

            // Act                           
            await target.MessageListenerTask.ShouldThrowAsync<ApplicationException>();
        }

        [TestMethod]
        public async Task Start_MessageConsumerThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            var exception = Dummy.CreateException<ApplicationException>();
            _messageConsumer = m =>
            {
                if (ReferenceEquals(message, m))
                {
                    throw exception;
                }
                throw new Exception("An unexpected message was received by the consumer");                
            };
            var target = GetAndStartTarget();

            // Act                           
            _producedMessages.Add(message);
            await target.MessageListenerTask.ShouldThrowAsync<ApplicationException>();
        }

        [TestMethod]        
        public async Task Start_StoppedWhileProducingMessage_ReturnsNull()
        {
            // Arrange            
            var target = GetAndStartTarget();

            // Act
            target.Stop();

            // Act                           
            (await target.MessageListenerTask).ShouldBeNull();
        }       

        [TestMethod]
        public async Task Start_NotificationReceived_CallsConsumer()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var target = GetAndStartTarget();

            // Act
            _producedNotifications.Add(notification);
            var actual = _consumedNotifications.Take(_cancellationToken);

            // Assert            
            actual.ShouldBe(notification);
            _producedNotifications.Add(_completionNotification);
            (await target.NotificationListenerTask.WithCancellation(_cancellationToken)).ShouldBe(_completionNotification);
            _channel.Verify(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task Start_MultipleNotificationsReceived_CallsConsumer()
        {
            // Arrange
            var notifications = new List<Notification>();
            var count = Dummy.CreateRandomInt(100) + 2;
            for (int i = 0; i < count; i++)
            {
                notifications.Add(
                    Dummy.CreateNotification(Event.Received));
            }
            var target = GetAndStartTarget();

            // Act
            foreach (var notification in notifications)
            {
                _producedNotifications.Add(notification);
            }

            // Assert
            _producedNotifications.Add(_completionNotification);
            (await target.NotificationListenerTask.WithCancellation(_cancellationToken)).ShouldBe(_completionNotification);
            _consumedNotifications.Count.ShouldBe(count);
            _channel.Verify(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()), Times.Exactly(count + 1));
        }

        [TestMethod]
        public async Task Start_ConsumerCompletedWhileProducingNotifications_StopsConsuming()
        {
            // Arrange
            var notifications = new List<Notification>();
            var count = Dummy.CreateRandomInt(500) + 2;
            var halfCount = count / 2;
            for (int i = 0; i < count; i++)
            {
                notifications.Add(
                    Dummy.CreateNotification(Event.Received));
            }

            int consumedCount = 0;
            _notificationConsumer = (m) =>
            {
                consumedCount++;
                if (consumedCount == halfCount)
                {
                    return TaskUtil.FalseCompletedTask;
                }
                return TaskUtil.TrueCompletedTask;
            };

            var target = GetAndStartTarget();

            // Act
            foreach (var notification in notifications)
            {
                _producedNotifications.Add(notification);
            }

            // Assert
            var unconsumedNotification = await target.NotificationListenerTask;
            unconsumedNotification.ShouldNotBeNull();
            _producedNotifications.ShouldNotContain(unconsumedNotification);
            _consumedNotifications.ShouldNotContain(unconsumedNotification);
            consumedCount.ShouldBe(halfCount);
            _channel.Verify(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()), Times.Exactly(halfCount));
            _producedNotifications.Count.ShouldBe(count - halfCount);
        }

        [TestMethod]
        public async Task Start_NotificationChannelThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var exception = Dummy.CreateException<ApplicationException>();
            _channel
                .Setup(m => m.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            var target = GetAndStartTarget();

            // Act                           
            await target.NotificationListenerTask.ShouldThrowAsync<ApplicationException>();
        }

        [TestMethod]
        public async Task Start_NotificationConsumerThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var exception = Dummy.CreateException<ApplicationException>();
            _notificationConsumer = m =>
            {
                if (ReferenceEquals(notification, m))
                {
                    throw exception;
                }
                throw new Exception("An unexpected notification was received by the consumer");
            };
            var target = GetAndStartTarget();

            // Act                           
            _producedNotifications.Add(notification);
            await target.NotificationListenerTask.ShouldThrowAsync<ApplicationException>();
        }

        [TestMethod]
        public async Task Start_StoppedWhileProducingNotification_ReturnsNull()
        {
            // Arrange            
            var target = GetAndStartTarget();

            // Act
            target.Stop();

            // Act                           
            (await target.NotificationListenerTask).ShouldBeNull();
        }

        [TestMethod]
        public async Task Start_CommandReceived_CallsConsumer()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreateTextContent());
            var target = GetAndStartTarget();

            // Act
            _producedCommands.Add(command);
            var actual = _consumedCommands.Take(_cancellationToken);

            // Assert            
            actual.ShouldBe(command);
            _producedCommands.Add(_completionCommand);
            (await target.CommandListenerTask.WithCancellation(_cancellationToken)).ShouldBe(_completionCommand);
            _channel.Verify(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task Start_MultipleCommandsReceived_CallsConsumer()
        {
            // Arrange
            var commands = new List<Command>();
            var count = Dummy.CreateRandomInt(100) + 2;
            for (int i = 0; i < count; i++)
            {
                commands.Add(
                    Dummy.CreateCommand());
            }
            var target = GetAndStartTarget();

            // Act
            foreach (var command in commands)
            {
                _producedCommands.Add(command);
            }

            // Assert
            _producedCommands.Add(_completionCommand);
            (await target.CommandListenerTask.WithCancellation(_cancellationToken)).ShouldBe(_completionCommand);
            _consumedCommands.Count.ShouldBe(count);
            _channel.Verify(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()), Times.Exactly(count + 1));
        }

        [TestMethod]
        public async Task Start_ConsumerCompletedWhileProducingCommands_StopsConsuming()
        {
            // Arrange
            var commands = new List<Command>();
            var count = Dummy.CreateRandomInt(500) + 2;
            var halfCount = count / 2;
            for (int i = 0; i < count; i++)
            {
                commands.Add(
                    Dummy.CreateCommand(Dummy.CreateTextContent()));
            }

            int consumedCount = 0;
            _commandConsumer = (m) =>
            {
                consumedCount++;
                if (consumedCount == halfCount)
                {
                    return TaskUtil.FalseCompletedTask;
                }
                return TaskUtil.TrueCompletedTask;
            };

            var target = GetAndStartTarget();

            // Act
            foreach (var command in commands)
            {
                _producedCommands.Add(command);
            }

            // Assert
            var unconsumedCommand = await target.CommandListenerTask;
            unconsumedCommand.ShouldNotBeNull();
            _producedCommands.ShouldNotContain(unconsumedCommand);
            _consumedCommands.ShouldNotContain(unconsumedCommand);
            consumedCount.ShouldBe(halfCount);
            _channel.Verify(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()), Times.Exactly(halfCount));
            _producedCommands.Count.ShouldBe(count - halfCount);
        }

        [TestMethod]
        public async Task Start_CommandChannelThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var exception = Dummy.CreateException<ApplicationException>();
            _channel
                .Setup(m => m.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            var target = GetAndStartTarget();

            // Act                           
            await target.CommandListenerTask.ShouldThrowAsync<ApplicationException>();
        }

        [TestMethod]
        public async Task Start_CommandConsumerThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreateTextContent());
            var exception = Dummy.CreateException<ApplicationException>();
            _commandConsumer = m =>
            {
                if (ReferenceEquals(command, m))
                {
                    throw exception;
                }
                throw new Exception("An unexpected command was received by the consumer");
            };
            var target = GetAndStartTarget();

            // Act                           
            _producedCommands.Add(command);
            await target.CommandListenerTask.ShouldThrowAsync<ApplicationException>();
        }

        [TestMethod]
        public async Task Start_StoppedWhileProducingCommand_ReturnsNull()
        {
            // Arrange            
            var target = GetAndStartTarget();

            // Act
            target.Stop();

            // Act                           
            (await target.CommandListenerTask).ShouldBeNull();
        }

    }
}
