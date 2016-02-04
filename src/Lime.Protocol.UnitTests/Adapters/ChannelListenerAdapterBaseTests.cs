using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Adapters;
using Lime.Protocol.Network;
using Lime.Protocol.Util;
using Moq;
using NUnit.Core;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Adapters
{
    [TestFixture]
    public class ChannelListenerAdapterBaseTests
    {


        private Mock<IMessageChannel> _messageChannel;
        private Mock<INotificationChannel> _notificationChannel;
        private Mock<ICommandChannel> _commandChannel;

        private BlockingCollection<Message> _producedMessages;
        private BlockingCollection<Notification> _producedNotifications;
        private BlockingCollection<Command> _producedCommands;

        private BlockingCollection<Message> _consumedMessages;
        private BlockingCollection<Notification> _consumedNotifications;
        private BlockingCollection<Command> _consumedCommands;

        private TaskCompletionSource<Message> _messageTcs;
        private TaskCompletionSource<Command> _commandTcs;
        private TaskCompletionSource<Notification> _notificationTcs;

        private Func<Message, Task> _messageConsumer;
        private Func<Notification, Task> _notificationConsumer;
        private Func<Command, Task> _commandConsumer;

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
                return TaskUtil.CompletedTask;
            };
            _notificationConsumer = n =>
            {
                _consumedNotifications.Add(n);
                if (ReferenceEquals(n, _completionNotification))
                {
                    _consumedNotifications.CompleteAdding();
                    _notificationTcs.SetResult(n);
                }
                return TaskUtil.CompletedTask;
            };
            _commandConsumer = c =>
            {
                _consumedCommands.Add(c);
                if (ReferenceEquals(c, _completionCommand))
                {
                    _consumedCommands.CompleteAdding();
                    _commandTcs.SetResult(c);
                }
                return TaskUtil.CompletedTask;
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

        private TestChannelListenerAdapterBase GetTarget()
        {
            return new TestChannelListenerAdapterBase(_messageChannel.Object, _notificationChannel.Object,
                _commandChannel.Object);
            
        }

        private TestChannelListenerAdapterBase GetTargetAndStartListenerTasks()
        {
            var target = GetTarget();
            target.StartListenerTasks(_messageConsumer, _notificationConsumer, _commandConsumer);
            return target;
        }

        [Test]
        public async Task StartListenerTasks_MessageReceived_CallsConsumer()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            var target = GetTargetAndStartListenerTasks();
           
            // Act
            _producedMessages.Add(message);            
            var actual = _consumedMessages.Take(_cancellationToken);

            // Assert
            actual.ShouldBe(message);
            _messageChannel.Verify(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));

            target.Dispose();
            await target.MessageListenerTask;
            await target.NotificationListenerTask;
            await target.CommandListenerTask;
        }

        [Test]
        public async Task StartListenerTasks_MultipleMessagesReceived_CallsConsumer()
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
            var target = GetTargetAndStartListenerTasks();

            // Act
            foreach (var message in messages)
            {
                _producedMessages.Add(message);
            }
            
            // Assert
            await _messageTcs.Task;
            _consumedMessages.Count.ShouldBe(count);
            _messageChannel.Verify(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Exactly(count));
            target.Dispose();
            await target.MessageListenerTask;
            await target.NotificationListenerTask;
            await target.CommandListenerTask;
        }

        [Test]
        public async Task StartListenerTasks_DisposedWhileConsumingMessages_StopsConsuming()
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

            ChannelListenerAdapterBase target = null;

            int consumedCount = 0;
            _messageConsumer = (m) =>
            {
                consumedCount++;
                if (consumedCount == halfCount)
                {
                    _messageTcs.TrySetResult(m);
                    target?.Dispose();
                }
                return TaskUtil.CompletedTask;
            };

            target = GetTargetAndStartListenerTasks();

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
            await target.NotificationListenerTask;
            await target.CommandListenerTask;
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public async Task StartListenerTasks_MessageChannelThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var exception = Dummy.CreateException<ApplicationException>();            
            _messageChannel
                .Setup(m => m.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            var target = GetTargetAndStartListenerTasks();

            // Act                           
            _messageChannel.Verify(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
            await target.MessageListenerTask;
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public async Task StartListenerTasks_MessageConsumerThrowsException_StopsListenerTaskAndThrows()
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
            var target = GetTargetAndStartListenerTasks();

            // Act                           
            _producedMessages.Add(message);
            _messageChannel.Verify(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
            await target.MessageListenerTask;
        }
        
        [Test]
        public async Task StartListenerTasks_NotificationReceived_CallsConsumer()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Authorized);
            var target = GetTargetAndStartListenerTasks();

            // Act
            _producedNotifications.Add(notification);
            var actual = _consumedNotifications.Take(_cancellationToken);

            // Assert
            actual.ShouldBe(notification);
            _notificationChannel.Verify(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));

            target.Dispose();
            await target.NotificationListenerTask;
            await target.NotificationListenerTask;
            await target.CommandListenerTask;
        }

        [Test]
        public async Task StartListenerTasks_MultipleNotificationsReceived_CallsConsumer()
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
            var target = GetTargetAndStartListenerTasks();

            // Act
            foreach (var notification in notifications)
            {
                _producedNotifications.Add(notification);
            }

            // Assert
            await _notificationTcs.Task;
            _consumedNotifications.Count.ShouldBe(count);
            _notificationChannel.Verify(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()), Times.Exactly(count));
            target.Dispose();
            await target.NotificationListenerTask;
            await target.NotificationListenerTask;
            await target.CommandListenerTask;
        }

        [Test]
        public async Task StartListenerTasks_DisposedWhileConsumingNotifications_StopsConsuming()
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

            ChannelListenerAdapterBase target = null;

            int consumedCount = 0;
            _notificationConsumer = (m) =>
            {
                consumedCount++;
                if (consumedCount == halfCount)
                {
                    _notificationTcs.TrySetResult(m);
                    target?.Dispose();
                }
                return TaskUtil.CompletedTask;
            };

            target = GetTargetAndStartListenerTasks();

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
            await target.NotificationListenerTask;
            await target.CommandListenerTask;
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public async Task StartListenerTasks_NotificationChannelThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var exception = Dummy.CreateException<ApplicationException>();
            _notificationChannel
                .Setup(m => m.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            var target = GetTargetAndStartListenerTasks();

            // Act                           
            _notificationChannel.Verify(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
            await target.NotificationListenerTask;
        }

        [Test]
        [ExpectedException(typeof(ApplicationException))]
        public async Task StartListenerTasks_NotificationConsumerThrowsException_StopsListenerTaskAndThrows()
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
            var target = GetTargetAndStartListenerTasks();

            // Act                           
            _producedNotifications.Add(notification);
            _notificationChannel.Verify(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
            await target.NotificationListenerTask;
        }

        [Test]
        public async Task StartListenerTasks_DisposedWhileConsumingCommands_StopsConsuming()
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

            ChannelListenerAdapterBase target = null;

            int consumedCount = 0;
            _commandConsumer = (m) =>
            {
                consumedCount++;
                if (consumedCount == halfCount)
                {
                    _commandTcs.TrySetResult(m);
                    target?.Dispose();
                }
                return TaskUtil.CompletedTask;
            };

            target = GetTargetAndStartListenerTasks();

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
            await target.NotificationListenerTask;
            await target.CommandListenerTask;
        }
    }

    public class TestChannelListenerAdapterBase : ChannelListenerAdapterBase
    {
        public TestChannelListenerAdapterBase(IMessageChannel messageChannel, INotificationChannel notificationChannel, ICommandChannel commandChannel) 
            : base(messageChannel, notificationChannel, commandChannel)
        {
        }

        public new void StartListenerTasks(Func<Message, Task> messageConsumer, Func<Notification, Task> notificationConsumer, Func<Command, Task> commandConsumer)
        {
            base.StartListenerTasks(messageConsumer, notificationConsumer, commandConsumer);
        }
    }
}
