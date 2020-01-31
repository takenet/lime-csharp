using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;
using Lime.Protocol.Util;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Listeners
{
    [TestFixture]
    public class BufferedChannelListenerTests
    {
        private Mock<IEstablishedReceiverChannel> _channel;
        private BufferBlock<Message> _producedMessages;
        private BufferBlock<Notification> _producedNotifications;
        private BufferBlock<Command> _producedCommands;
        private BufferBlock<Message> _consumedMessages;
        private BufferBlock<Notification> _consumedNotifications;
        private BufferBlock<Command> _consumedCommands;
        private Func<Message, CancellationToken, Task<bool>> _messageConsumer;
        private Func<Notification, CancellationToken, Task<bool>> _notificationConsumer;
        private Func<Command, CancellationToken, Task<bool>> _commandConsumer;

        private CancellationToken _cancellationToken;
        private Message _completionMessage;
        private Notification _completionNotification;
        private Command _completionCommand;
        private List<BufferedChannelListener> _createdTargets;

        [SetUp]
        public void SetUp()
        {
            _channel = new Mock<IEstablishedReceiverChannel>();
            
            _producedMessages = new BufferBlock<Message>(new DataflowBlockOptions() { BoundedCapacity = -1 });
            _producedNotifications = new BufferBlock<Notification>(new DataflowBlockOptions() { BoundedCapacity = -1 });
            _producedCommands = new BufferBlock<Command>(new DataflowBlockOptions() { BoundedCapacity = -1 });

            _channel
                .Setup(m => m.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) => _producedMessages.ReceiveAsync(cancellationToken));
            _channel
                .Setup(m => m.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) => _producedNotifications.ReceiveAsync(cancellationToken));
            _channel
                .Setup(m => m.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) => _producedCommands.ReceiveAsync(cancellationToken));

            _consumedMessages = new BufferBlock<Message>(new DataflowBlockOptions() { BoundedCapacity = -1 });
            _consumedNotifications = new BufferBlock<Notification>(new DataflowBlockOptions() { BoundedCapacity = -1 });
            _consumedCommands = new BufferBlock<Command>(new DataflowBlockOptions() { BoundedCapacity = -1 });

            _cancellationToken = TimeSpan.FromSeconds(15).ToCancellationToken();

            _messageConsumer = (m, ct) =>
            {                
                if (ReferenceEquals(m, _completionMessage))
                {
                    _consumedMessages.Complete();
                    return TaskUtil.FalseCompletedTask;
                }
                _consumedMessages.SendAsync(m, _cancellationToken);
                return TaskUtil.TrueCompletedTask;
            };
            _notificationConsumer = (n, ct) =>
            {                
                if (ReferenceEquals(n, _completionNotification))
                {
                    _consumedNotifications.Complete();
                    return TaskUtil.FalseCompletedTask;
                }
                _consumedNotifications.SendAsync(n, _cancellationToken);
                return TaskUtil.TrueCompletedTask;
            };
            _commandConsumer = (c, ct) =>
            {                
                if (ReferenceEquals(c, _completionCommand))
                {
                    _consumedCommands.Complete();
                    return TaskUtil.FalseCompletedTask;
                }
                _consumedCommands.SendAsync(c, _cancellationToken);
                return TaskUtil.TrueCompletedTask;
            };

            _completionMessage = Dummy.CreateMessage(Dummy.CreateTextContent());
            _completionNotification = Dummy.CreateNotification(Event.Consumed);
            _completionCommand = Dummy.CreateCommand();
        }

        [TearDown]
        public void TearDown()
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

        protected BufferedChannelListener GetAndStartTarget()
        {
            if (_createdTargets == null)
            {
                _createdTargets = new List<BufferedChannelListener>();
            }

            var target = new BufferedChannelListener(_messageConsumer, _notificationConsumer, _commandConsumer, -1);
            target.Start(_channel.Object);
            _createdTargets.Add(target);
            return target;
        }

        [Test]
        public async Task Start_MessageReceived_CallsConsumer()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            var target = GetAndStartTarget();

            // Act
            await _producedMessages.SendAsync(message);
            var actual = await _consumedMessages.ReceiveAsync(_cancellationToken);

            // Assert            
            actual.ShouldBe(message);            
            await _producedMessages.SendAsync(_completionMessage);            
            (await target.MessageListenerTask.WithCancellation(_cancellationToken)).ShouldBe(_completionMessage);
            _channel.Verify(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Test]
        public async Task Start_MultipleMessagesReceived_CallsConsumer()
        {
            // Arrange
            var messages = new List<Message>();
            var count = 100;
            for (int i = 0; i < count; i++)
            {
                messages.Add(
                    Dummy.CreateMessage(Dummy.CreateTextContent()));
            }            
            var target = GetAndStartTarget();            

            // Act
            foreach (var message in messages)
            {
                await _producedMessages.SendAsync(message);
            }

            // Assert
            await _producedMessages.SendAsync(_completionMessage);
            (await target.MessageListenerTask.WithCancellation(_cancellationToken)).ShouldBe(_completionMessage);            
            _consumedMessages.Count.ShouldBe(count);
            _channel.Verify(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Exactly(count + 2));                        
        }

        [Test]
        public async Task Start_ConsumerCompletedWhileProducingMessages_StopsConsuming()
        {
            // Arrange
            var messages = new List<Message>();
            var count = 500;
            var halfCount = count/2;
            for (int i = 0; i < count; i++)
            {
                messages.Add(
                    Dummy.CreateMessage(Dummy.CreateTextContent()));
            }

            int consumedCount = 0;
            _messageConsumer = (m, ct) =>
            {
                Interlocked.Increment(ref consumedCount);
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
                await _producedMessages.SendAsync(message);
            }

            // Assert
            var unconsumedMessage = await target.MessageListenerTask;
            unconsumedMessage.ShouldNotBeNull();
            _producedMessages.TryReceiveAll(out var producedItems);
            producedItems?.ShouldNotContain(unconsumedMessage);
            _consumedMessages.TryReceiveAll(out var consumedItems);
            consumedItems?.ShouldNotContain(unconsumedMessage);
            consumedCount.ShouldBe(halfCount);
            _channel.Verify(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.AtLeast(halfCount));
                    }

        [Test]        
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

        [Test]
        public async Task Start_MessageConsumerThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            var exception = Dummy.CreateException<ApplicationException>();
            _messageConsumer = (m, ct) =>
            {
                if (ReferenceEquals(message, m))
                {
                    throw exception;
                }
                throw new Exception("An unexpected message was received by the consumer");                
            };
            var target = GetAndStartTarget();

            // Act                           
            await _producedMessages.SendAsync(message);
            await target.MessageListenerTask.ShouldThrowAsync<ApplicationException>();
        }

        [Test]        
        public async Task Start_StoppedWhileProducingMessage_ReturnsNull()
        {
            // Arrange            
            var target = GetAndStartTarget();

            // Act
            target.Stop();

            // Act                           
            (await target.MessageListenerTask).ShouldBeNull();
        }       

        [Test]
        public async Task Start_NotificationReceived_CallsConsumer()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var target = GetAndStartTarget();

            // Act
            await _producedNotifications.SendAsync(notification);
            var actual = await _consumedNotifications.ReceiveAsync(_cancellationToken);

            // Assert            
            actual.ShouldBe(notification);
            await _producedNotifications.SendAsync(_completionNotification);
            (await target.NotificationListenerTask.WithCancellation(_cancellationToken)).ShouldBe(_completionNotification);
            _channel.Verify(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Test]
        public async Task Start_MultipleNotificationsReceived_CallsConsumer()
        {
            // Arrange
            var notifications = new List<Notification>();
            var count = 100;
            for (int i = 0; i < count; i++)
            {
                notifications.Add(
                    Dummy.CreateNotification(Event.Received));
            }
            var target = GetAndStartTarget();

            // Act
            foreach (var notification in notifications)
            {
                await _producedNotifications.SendAsync(notification);
            }

            // Assert
            await _producedNotifications.SendAsync(_completionNotification);
            (await target.NotificationListenerTask.WithCancellation(_cancellationToken)).ShouldBe(_completionNotification);
            _consumedNotifications.Count.ShouldBe(count);
            _channel.Verify(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()), Times.Exactly(count + 2));
        }

        [Test]
        public async Task Start_ConsumerCompletedWhileProducingNotifications_StopsConsuming()
        {
            // Arrange
            var notifications = new List<Notification>();
            var count = 500;
            var halfCount = count / 2;
            for (int i = 0; i < count; i++)
            {
                notifications.Add(
                    Dummy.CreateNotification(Event.Received));
            }

            int consumedCount = 0;
            _notificationConsumer = (n, ct) =>
            {
                Interlocked.Increment(ref consumedCount);
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
                await _producedNotifications.SendAsync(notification);
            }

            // Assert
            var unconsumedNotification = await target.NotificationListenerTask;
            unconsumedNotification.ShouldNotBeNull();
            _producedNotifications.TryReceiveAll(out var producedItems);
            producedItems?.ShouldNotContain(unconsumedNotification);
            _consumedNotifications.TryReceiveAll(out var consumedItems);
            consumedItems?.ShouldNotContain(unconsumedNotification);
            consumedCount.ShouldBe(halfCount);
            _channel.Verify(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()), Times.AtLeast(halfCount));
        }

        [Test]
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

        [Test]
        public async Task Start_NotificationConsumerThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var exception = Dummy.CreateException<ApplicationException>();
            _notificationConsumer = (m, ct) =>
            {
                if (ReferenceEquals(notification, m))
                {
                    throw exception;
                }
                throw new Exception("An unexpected notification was received by the consumer");
            };
            var target = GetAndStartTarget();

            // Act                           
            await _producedNotifications.SendAsync(notification);
            await target.NotificationListenerTask.ShouldThrowAsync<ApplicationException>();
        }

        [Test]
        public async Task Start_StoppedWhileProducingNotification_ReturnsNull()
        {
            // Arrange            
            var target = GetAndStartTarget();

            // Act
            target.Stop();

            // Act                           
            (await target.NotificationListenerTask).ShouldBeNull();
        }

        [Test]
        public async Task Start_CommandReceived_CallsConsumer()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreateTextContent());
            var target = GetAndStartTarget();

            // Act
            await _producedCommands.SendAsync(command);
            var actual = await _consumedCommands.ReceiveAsync(_cancellationToken);

            // Assert            
            actual.ShouldBe(command);
            await _producedCommands.SendAsync(_completionCommand);
            (await target.CommandListenerTask.WithCancellation(_cancellationToken)).ShouldBe(_completionCommand);
            _channel.Verify(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Test]
        public async Task Start_MultipleCommandsReceived_CallsConsumer()
        {
            // Arrange
            var commands = new List<Command>();
            var count = 100;
            for (int i = 0; i < count; i++)
            {
                commands.Add(
                    Dummy.CreateCommand());
            }
            var target = GetAndStartTarget();

            // Act
            foreach (var command in commands)
            {
                await _producedCommands.SendAsync(command);
            }

            // Assert
            await _producedCommands.SendAsync(_completionCommand);
            (await target.CommandListenerTask.WithCancellation(_cancellationToken)).ShouldBe(_completionCommand);
            _consumedCommands.Count.ShouldBe(count);
            _channel.Verify(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()), Times.Exactly(count + 2));
        }

        [Test]
        public async Task Start_ConsumerCompletedWhileProducingCommands_StopsConsuming()
        {
            // Arrange
            var commands = new List<Command>();
            var count = 500;
            var halfCount = count / 2;
            for (int i = 0; i < count; i++)
            {
                commands.Add(
                    Dummy.CreateCommand(Dummy.CreateTextContent()));
            }

            int consumedCount = 0;
            _commandConsumer = (c, ct) =>
            {
                Interlocked.Increment(ref consumedCount);
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
                await _producedCommands.SendAsync(command);
            }

            // Assert
            var unconsumedCommand = await target.CommandListenerTask;
            unconsumedCommand.ShouldNotBeNull();
            _producedCommands.TryReceiveAll(out var producedItems);
            producedItems?.ShouldNotContain(unconsumedCommand);
            _consumedCommands.TryReceiveAll(out var consumedItems);
            consumedItems?.ShouldNotContain(unconsumedCommand);
            consumedCount.ShouldBe(halfCount);
            _channel.Verify(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()), Times.AtLeast(halfCount));
                    }

        [Test]
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

        [Test]
        public async Task Start_CommandConsumerThrowsException_StopsListenerTaskAndThrows()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreateTextContent());
            var exception = Dummy.CreateException<ApplicationException>();
            _commandConsumer = (m, ct) =>
            {
                if (ReferenceEquals(command, m))
                {
                    throw exception;
                }
                throw new Exception("An unexpected command was received by the consumer");
            };
            var target = GetAndStartTarget();

            // Act                           
            await _producedCommands.SendAsync(command);
            await target.CommandListenerTask.ShouldThrowAsync<ApplicationException>();
        }

        [Test]
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