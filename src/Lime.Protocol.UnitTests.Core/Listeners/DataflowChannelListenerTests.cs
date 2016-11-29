using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Listeners
{
    [TestFixture]
    public class DataflowChannelListenerTests
    {
        protected Mock<IEstablishedReceiverChannel> _channel;
        
        protected BlockingCollection<Message> _producedMessages;
        protected BlockingCollection<Notification> _producedNotifications;
        protected BlockingCollection<Command> _producedCommands;

        private BufferBlock<Message> _messageBufferBlock;        
        private BufferBlock<Notification> _notificationBufferBlock;
        private BufferBlock<Command> _commandBufferBlock;
        private CancellationTokenSource _cts;

        [SetUp]
        public void Setup()
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

            _cts = new CancellationTokenSource();
            var options = new DataflowBlockOptions()
            {
                CancellationToken = _cts.Token
            };
            _messageBufferBlock = new BufferBlock<Message>(options);
            _notificationBufferBlock = new BufferBlock<Notification>(options);
            _commandBufferBlock = new BufferBlock<Command>(options);
        }

        [TearDown]
        public void TearDown()
        {
            _cts.Dispose();
            _channel = null;
            _messageBufferBlock = null;
            _notificationBufferBlock = null;
            _commandBufferBlock = null;
        }

        private DataflowChannelListener GetAndStartTarget()
        {
            var target = new DataflowChannelListener(_messageBufferBlock, _notificationBufferBlock, _commandBufferBlock);

            target.Start(_channel.Object);
            return target;            
        }
        
        [Test]
        public async Task Start_MessageReceived_SendsToBuffer()
        {
            // Arrange            
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            var target = GetAndStartTarget();

            // Act
            _producedMessages.Add(message);

            // Assert
            Message actual = await _messageBufferBlock.ReceiveAsync();
            actual.ShouldBe(message);
            target.Stop();
        }

        [Test]
        public async Task Start_MultipleMessagesReceived_SendsToBuffer()
        {
            // Arrange
            var messages = new List<Message>();
            var count = Dummy.CreateRandomInt(100);
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
            for (int i = 0; i < count; i++)
            {
                var actual = await _messageBufferBlock.ReceiveAsync();
                messages.ShouldContain(actual);
            }
        }

        [Test]
        public async Task Start_CompletedMessageBufferBlock_StopsConsumerTask()
        {
            // Arrange            
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            var target = GetAndStartTarget();

            // Act
            _messageBufferBlock.Complete();
            _producedMessages.Add(message);

            // Assert
            (await target.MessageListenerTask.WithCancellation(_cts.Token)).ShouldBe(message);
        }

        [Test]
        public async Task Start_NotificationReceived_SendsToBuffer()
        {
            // Arrange            
            var notification = Dummy.CreateNotification(Event.Authorized);
            var target = GetAndStartTarget();

            // Act
            _producedNotifications.Add(notification);

            // Assert
            Notification actual = await _notificationBufferBlock.ReceiveAsync();
            actual.ShouldBe(notification);
            target.Stop();            
        }

        [Test]
        public async Task Start_MultipleNotificationsReceived_SendsToBuffer()
        {
            // Arrange
            var notifications = new List<Notification>();
            var count = Dummy.CreateRandomInt(100);
            for (int i = 0; i < count; i++)
            {
                notifications.Add(
                    Dummy.CreateNotification(Event.Authorized));
            }
            var target = GetAndStartTarget();

            // Act
            foreach (var notification in notifications)
            {
                _producedNotifications.Add(notification);
            }

            // Assert
            for (int i = 0; i < count; i++)
            {
                var actual = await _notificationBufferBlock.ReceiveAsync();
                notifications.ShouldContain(actual);
            }            
        }

        [Test]
        public async Task Start_CompletedNotificationBufferBlock_StopsConsumerTask()
        {
            // Arrange            
            var notification = Dummy.CreateNotification(Event.Authorized);
            var target = GetAndStartTarget();

            // Act
            _notificationBufferBlock.Complete();
            _producedNotifications.Add(notification);

            // Assert
            (await target.NotificationListenerTask.WithCancellation(_cts.Token)).ShouldBe(notification);
        }
        
        [Test]
        public async Task Start_CommandReceived_SendsToBuffer()
        {
            // Arrange            
            var command = Dummy.CreateCommand(Dummy.CreateTextContent());
            var target = GetAndStartTarget();

            // Act
            _producedCommands.Add(command);

            // Assert
            Command actual = await _commandBufferBlock.ReceiveAsync();
            actual.ShouldBe(command);
            target.Stop();
        }

        [Test]
        public async Task Start_MultipleCommandsReceived_SendsToBuffer()
        {
            // Arrange
            var commands = new List<Command>();
            var count = Dummy.CreateRandomInt(100);
            for (int i = 0; i < count; i++)
            {
                commands.Add(
                    Dummy.CreateCommand(Dummy.CreateTextContent()));
            }
            var target = GetAndStartTarget();

            // Act
            foreach (var command in commands)
            {
                _producedCommands.Add(command);
            }

            // Assert
            for (int i = 0; i < count; i++)
            {
                var actual = await _commandBufferBlock.ReceiveAsync();
                commands.ShouldContain(actual);
            }
        }

        [Test]
        public async Task Start_CompletedCommandBufferBlock_StopsConsumerTask()
        {
            // Arrange            
            var command = Dummy.CreateCommand(Dummy.CreateTextContent());
            var target = GetAndStartTarget();

            // Act
            _commandBufferBlock.Complete();
            _producedCommands.Add(command);

            // Assert
            (await target.CommandListenerTask.WithCancellation(_cts.Token)).ShouldBe(command);
        }
    }
}