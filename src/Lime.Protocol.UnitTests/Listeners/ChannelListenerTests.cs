using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;
using Lime.Protocol.Util;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Listeners
{
    
    [TestFixture]
    public class ChannelListenerTests
    {
        protected Mock<IMessageChannel> _messageChannel;
        protected Mock<INotificationChannel> _notificationChannel;
        protected Mock<ICommandChannel> _commandChannel;

        protected BlockingCollection<Message> _producedMessages;
        protected BlockingCollection<Notification> _producedNotifications;
        protected BlockingCollection<Command> _producedCommands;

        protected BlockingCollection<Message> _consumedMessages;
        protected BlockingCollection<Notification> _consumedNotifications;
        protected BlockingCollection<Command> _consumedCommands;

        private TaskCompletionSource<Message> _messageTcs;
        private TaskCompletionSource<Command> _commandTcs;
        private TaskCompletionSource<Notification> _notificationTcs;

        protected Func<Message, Task<bool>> _messageConsumer;
        protected Func<Notification, Task<bool>> _notificationConsumer;
        protected Func<Command, Task<bool>> _commandConsumer;

        private CancellationToken _cancellationToken;

        private Message _completionMessage;
        private Notification _completionNotification;
        private Command _completionCommand;


        [SetUp]
        public void Setup()
        {            
            _messageChannel = new Mock<IMessageChannel>();
            _notificationChannel = new Mock<INotificationChannel>();
            _commandChannel = new Mock<ICommandChannel>();

            _producedMessages = new BlockingCollection<Message>();
            _producedNotifications = new BlockingCollection<Notification>();
            _producedCommands = new BlockingCollection<Command>();

            _messageChannel
                .Setup(m => m.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) => _producedMessages.Take(cancellationToken).AsCompletedTask());
            _notificationChannel
                .Setup(m => m.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) => _producedNotifications.Take(cancellationToken).AsCompletedTask());
            _commandChannel
                .Setup(m => m.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) => _producedCommands.Take(cancellationToken).AsCompletedTask());

            _consumedMessages = new BlockingCollection<Message>();
            _consumedNotifications = new BlockingCollection<Notification>();
            _consumedCommands = new BlockingCollection<Command>();

            _messageTcs= new TaskCompletionSource<Message>();
            _notificationTcs = new TaskCompletionSource<Notification>();
            _commandTcs = new TaskCompletionSource<Command>();

            _messageConsumer = m =>
            {
                _consumedMessages.Add(m);
                if (ReferenceEquals(m, _completionMessage))
                {
                    _consumedMessages.CompleteAdding();
                    _messageTcs.SetResult(m);
                }
                return TaskUtil.TrueCompletedTask;
            };
            _notificationConsumer = n =>
            {
                _consumedNotifications.Add(n);
                if (ReferenceEquals(n, _completionNotification))
                {
                    _consumedNotifications.CompleteAdding();
                    _notificationTcs.SetResult(n);
                }
                return TaskUtil.TrueCompletedTask;
            };
            _commandConsumer = c =>
            {
                _consumedCommands.Add(c);
                if (ReferenceEquals(c, _completionCommand))
                {
                    _consumedCommands.CompleteAdding();
                    _commandTcs.SetResult(c);
                }
                return TaskUtil.TrueCompletedTask;
            };

            _completionMessage = Dummy.CreateMessage(Dummy.CreateTextContent());
            _completionNotification = Dummy.CreateNotification(Event.Authorized);
            _completionCommand = Dummy.CreateCommand();

            _cancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
            _cancellationToken.Register(() =>
            {
                _messageTcs.TrySetCanceled();
                _notificationTcs.TrySetCanceled();
                _commandTcs.TrySetCanceled();
            });
        }

        [TearDown]   
        public void TearDown()
        {
            _messageChannel = null;
            _notificationChannel = null;
            _commandChannel = null;
            _producedMessages = null;
            _producedNotifications = null;
            _producedCommands = null;
            _consumedMessages = null;
            _consumedNotifications = null;
            _consumedCommands = null;
            _cancellationToken = CancellationToken.None;
        }


        protected ChannelListener GetAndStartTarget()
        {
            var target = new ChannelListener(_messageChannel.Object, _notificationChannel.Object,
                _commandChannel.Object,
                _messageConsumer, _notificationConsumer, _commandConsumer);

            target.Start();
            return target;
        }

        [Test]
        public async Task Start_MessageReceived_CallsConsumer()
        {
            // Arrange            
            var target = GetAndStartTarget();
           
            // Act
            _producedMessages.Add(_completionMessage);            
            var actual = _consumedMessages.Take(_cancellationToken);

            // Assert
            actual.ShouldBe(_completionMessage);
            _messageChannel.Verify(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Between(1, 2, Range.Inclusive));

            target.Dispose();
            await target.MessageListenerTask;
            await target.NotificationListenerTask;
            await target.CommandListenerTask;
        }

        [Test]
        public async Task Start_MultipleMessagesReceived_CallsConsumer()
        {
            // Arrange
            var messages = new List<Message>();
            var count = Dummy.CreateRandomInt(500) + 2;
            for (int i = 0; i < count - 1; i++)
            {
                messages.Add(
                    Dummy.CreateMessage(Dummy.CreateTextContent()));
            }
            messages.Add(_completionMessage);            
            var target = GetAndStartTarget();

            // Act
            foreach (var message in messages)
            {
                _producedMessages.Add(message);
            }
            
            // Assert
            await _messageTcs.Task;
            _consumedMessages.Count.ShouldBe(count);
            _messageChannel.Verify(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Between(count, count+1, Range.Inclusive));
            target.Dispose();
            await target.MessageListenerTask;
            await target.NotificationListenerTask;
            await target.CommandListenerTask;
        }

        [Test]
        public async Task Start_StoppedhileConsumingMessages_StopsConsuming()
        {
            // Arrange
            var messages = new List<Message>();
            var count = Dummy.CreateRandomInt(500) + 2;
            var halfCount = count/2;
            for (int i = 0; i < count - 1; i++)
            {
                messages.Add(
                    Dummy.CreateMessage(Dummy.CreateTextContent()));
            }

            ChannelListener target = null;

            int consumedCount = 0;
            _messageConsumer = (m) =>
            {
                consumedCount++;
                if (consumedCount == halfCount)
                {
                    _messageTcs.TrySetResult(m);
                    return TaskUtil.FalseCompletedTask;
                }
                return TaskUtil.TrueCompletedTask;
            };

            target = GetAndStartTarget();

            // Act
            foreach (var message in messages)
            {
                _producedMessages.Add(message);
            }

            // Assert
            await _messageTcs.Task;
            consumedCount.ShouldBe(halfCount);
            _messageChannel.Verify(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Exactly(halfCount));
            _producedMessages.Count.ShouldBe(count - halfCount - 1);
            await target.MessageListenerTask;            
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public async Task Start_MessageChannelThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var exception = Dummy.CreateException<ApplicationException>();            
            _messageChannel
                .Setup(m => m.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            var target = GetAndStartTarget();

            // Act                           
            await target.MessageListenerTask;
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
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
                throw new AssertionException("An unexpected message was received by the consumer");                
            };
            var target = GetAndStartTarget();

            // Act                           
            _producedMessages.Add(message);
            await target.MessageListenerTask;
        }
        
        [Test]
        public async Task Start_NotificationReceived_CallsConsumer()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Authorized);
            var target = GetAndStartTarget();

            // Act
            _producedNotifications.Add(notification);
            var actual = _consumedNotifications.Take(_cancellationToken);

            // Assert
            actual.ShouldBe(notification);
            _notificationChannel.Verify(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()), Times.Between(1, 2, Range.Inclusive));

            target.Dispose();
            await target.NotificationListenerTask;
            await target.NotificationListenerTask;
            await target.CommandListenerTask;
        }

        [Test]
        public async Task Start_MultipleNotificationsReceived_CallsConsumer()
        {
            // Arrange
            var notifications = new List<Notification>();
            var count = Dummy.CreateRandomInt(500) + 2;
            for (int i = 0; i < count - 1; i++)
            {
                notifications.Add(
                    Dummy.CreateNotification(Event.Authorized));
            }
            notifications.Add(_completionNotification);
            var target = GetAndStartTarget();

            // Act
            foreach (var notification in notifications)
            {
                _producedNotifications.Add(notification);
            }

            // Assert
            await _notificationTcs.Task;
            _consumedNotifications.Count.ShouldBe(count);
            _notificationChannel.Verify(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()), Times.Between(count, count + 1, Range.Inclusive));
            target.Dispose();
            await target.NotificationListenerTask;
            await target.NotificationListenerTask;
            await target.CommandListenerTask;
        }

        [Test]
        public async Task Start_StoppedWhileConsumingNotifications_StopsConsuming()
        {
            // Arrange
            var notifications = new List<Notification>();
            var count = Dummy.CreateRandomInt(500) + 2;
            var halfCount = count / 2;
            for (int i = 0; i < count - 1; i++)
            {
                notifications.Add(
                    Dummy.CreateNotification(Event.Authorized));
            }

            ChannelListener target = null;

            int consumedCount = 0;
            _notificationConsumer = (m) =>
            {
                consumedCount++;
                if (consumedCount == halfCount)
                {
                    _notificationTcs.TrySetResult(m);
                    return TaskUtil.FalseCompletedTask;
                }
                return TaskUtil.TrueCompletedTask;
            };

            target = GetAndStartTarget();

            // Act
            foreach (var notification in notifications)
            {
                _producedNotifications.Add(notification);
            }

            // Assert
            await _notificationTcs.Task;
            consumedCount.ShouldBe(halfCount);
            _notificationChannel.Verify(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()), Times.Exactly(halfCount));
            _producedNotifications.Count.ShouldBe(count - halfCount - 1);
            await target.NotificationListenerTask;
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public async Task Start_NotificationChannelThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var exception = Dummy.CreateException<ApplicationException>();
            _notificationChannel
                .Setup(m => m.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            var target = GetAndStartTarget();

            // Act                           
            await target.NotificationListenerTask;
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public async Task Start_NotificationConsumerThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Authorized);
            var exception = Dummy.CreateException<ApplicationException>();
            _notificationConsumer = m =>
            {
                if (ReferenceEquals(notification, m))
                {
                    throw exception;
                }
                throw new AssertionException("An unexpected notification was received by the consumer");
            };
            var target = GetAndStartTarget();

            // Act                           
            _producedNotifications.Add(notification);
            await target.NotificationListenerTask;
        }
        
        [Test]
        public async Task Start_CommandReceived_CallsConsumer()
        {
            // Arrange            
            var target = GetAndStartTarget();

            // Act
            _producedCommands.Add(_completionCommand);
            var actual = _consumedCommands.Take(_cancellationToken);

            // Assert
            actual.ShouldBe(_completionCommand);
            _commandChannel.Verify(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()), Times.Between(1, 2, Range.Inclusive));

            target.Dispose();
            await target.CommandListenerTask;
            await target.NotificationListenerTask;
            await target.CommandListenerTask;
        }

        [Test]
        public async Task Start_MultipleCommandsReceived_CallsConsumer()
        {
            // Arrange
            var commands = new List<Command>();
            var count = Dummy.CreateRandomInt(500) + 2;
            for (int i = 0; i < count - 1; i++)
            {
                commands.Add(
                    Dummy.CreateCommand(Dummy.CreateTextContent()));
            }
            commands.Add(_completionCommand);
            var target = GetAndStartTarget();

            // Act
            foreach (var command in commands)
            {
                _producedCommands.Add(command);
            }

            // Assert
            await _commandTcs.Task;
            _consumedCommands.Count.ShouldBe(count);
            _commandChannel.Verify(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()), Times.Between(count, count + 1, Range.Inclusive));
            target.Dispose();
            await target.CommandListenerTask;
            await target.NotificationListenerTask;
            await target.CommandListenerTask;
        }

        [Test]
        public async Task Start_StoppedWhileConsumingCommands_StopsConsuming()
        {
            // Arrange
            var commands = new List<Command>();
            var count = Dummy.CreateRandomInt(500) + 2;
            var halfCount = count / 2;
            for (int i = 0; i < count - 1; i++)
            {
                commands.Add(
                    Dummy.CreateCommand(Dummy.CreateTextContent()));
            }

            ChannelListener target = null;

            int consumedCount = 0;
            _commandConsumer = (m) =>
            {
                consumedCount++;
                if (consumedCount == halfCount)
                {
                    _commandTcs.TrySetResult(m);
                    return TaskUtil.FalseCompletedTask;
                }
                return TaskUtil.TrueCompletedTask;
            };

            target = GetAndStartTarget();

            // Act
            foreach (var command in commands)
            {
                _producedCommands.Add(command);
            }

            // Assert
            await _commandTcs.Task;
            consumedCount.ShouldBe(halfCount);
            _commandChannel.Verify(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()), Times.Exactly(halfCount));
            _producedCommands.Count.ShouldBe(count - halfCount - 1);
            await target.CommandListenerTask;
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public async Task Start_CommandChannelThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var exception = Dummy.CreateException<ApplicationException>();
            _commandChannel
                .Setup(m => m.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            var target = GetAndStartTarget();

            // Act                           
            await target.CommandListenerTask;
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
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
                throw new AssertionException("An unexpected command was received by the consumer");
            };
            var target = GetAndStartTarget();

            // Act                           
            _producedCommands.Add(command);
            await target.CommandListenerTask;
        }
    }
}
