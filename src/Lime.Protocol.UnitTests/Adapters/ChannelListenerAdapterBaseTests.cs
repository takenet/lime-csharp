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
                .Returns(() => _producedMessages.Take().AsCompletedTask());
            _notificationChannel
                .Setup(m => m.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .Returns(() => _producedNotifications.Take().AsCompletedTask());
            _commandChannel
                .Setup(m => m.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .Returns(() => _producedCommands.Take().AsCompletedTask());

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
        public void StartListenerTasks_MessageReceived_CallsConsumer()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            var target = GetTargetAndStartListenerTasks();
           
            // Act
            _producedMessages.Add(message);            
            var actual = _consumedMessages.Take(_cancellationToken);

            // Assert
            actual.ShouldBe(message);
            target.Dispose();
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
