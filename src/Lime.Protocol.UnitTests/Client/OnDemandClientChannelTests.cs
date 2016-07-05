using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.Threading.Tasks;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Moq;
using System.Threading;
using Shouldly;
using Lime.Protocol.Util;

namespace Lime.Protocol.UnitTests.Client
{
    [TestFixture]
    public class OnDemandClientChannelTests
    {
        private TimeSpan _sendTimeout;
        private CancellationToken _cancellationToken;
        private string _sessionId;
        private Mock<IClientChannelBuilder> _clientChannelBuilder;
        private Mock<IEstablishedClientChannelBuilder> _establishedClientChannelBuilder;
        private Mock<IClientChannel> _clientChannel;
        private Mock<IDisposable> _clientChannelDisposable;
        private Mock<IDisposable> _disposableClientChannel;
        private Mock<ITransport> _transport;

        [SetUp]
        public void Setup()
        {
            _sendTimeout = TimeSpan.FromSeconds(5);
            _cancellationToken = _sendTimeout.ToCancellationToken();
            _sessionId = EnvelopeId.NewId();            
            _transport = new Mock<ITransport>();
            _transport
                .SetupGet(t => t.IsConnected)
                .Returns(true);
            _clientChannel = new Mock<IClientChannel>();
            _clientChannelDisposable = _clientChannel.As<IDisposable>();
            _clientChannel
                .SetupGet(c => c.SessionId)
                .Returns(_sessionId);
            _clientChannel
                .SetupGet(c => c.Transport)
                .Returns(_transport.Object);
            _clientChannel
                .SetupGet(c => c.State)
                .Returns(SessionState.Established);
            _disposableClientChannel = _clientChannel.As<IDisposable>();
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
            ChannelInformation channelInformation = null;
            target.ChannelCreatedHandlers.Add((c) =>
            {
                channelInformation = c;
                return TaskUtil.CompletedTask;
            });

            // Act
            await target.SendMessageAsync(message, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            _clientChannel.Verify(c => c.SendMessageAsync(message, CancellationToken.None), Times.Once());
            channelInformation.ShouldNotBeNull();
            channelInformation.SessionId.ShouldBe(_sessionId);
            channelInformation.State.ShouldBe(SessionState.Established);
        }

        [Test]
        public async Task SendMessageAsync_NotEstablishedChannelMultipleCalls_BuildChannelOnceAndSends()
        {
            // Arrange
            var count = Dummy.CreateRandomInt(500) + 1;;
            var messages = new Message[count];
            for (int i = 0; i < count; i++)
            {
                messages[i] = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            }
            
            var target = GetTarget();

            // Act
            await Task.WhenAll(
                Enumerable
                    .Range(0, count)
                    .Select(i => Task.Run(() => target.SendMessageAsync(messages[i], CancellationToken.None))));


            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            foreach (var message in messages)
            {
                _clientChannel.Verify(c => c.SendMessageAsync(message, CancellationToken.None), Times.Once());
            }            
        }

        [Test]
        public async Task SendMessageAsync_ChannelCreatedHandlerThrowsException_ThrowsExceptionToTheCaller()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreatedHandlers.Add((c) =>
            {
                throw exception;
            });

            // Act
            await target.SendMessageAsync(message, CancellationToken.None).ShouldThrowAsync<ApplicationException>();
        }

        [Test]
        public async Task SendMessageAsync_MultipleChannelCreatedHandlerThrowsException_ThrowsAggregateExceptionToTheCaller()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception1 = Dummy.CreateException<ApplicationException>();
            target.ChannelCreatedHandlers.Add((c) =>
            {
                throw exception1;
            });
            var exception2 = Dummy.CreateException<ApplicationException>();
            target.ChannelCreatedHandlers.Add((c) =>
            {
                throw exception2;
            });

            // Act
            try
            {
                await target.SendMessageAsync(message, CancellationToken.None);
            }
            catch (AggregateException ex)
            {
                ex.InnerExceptions.Count.ShouldBe(2);
                ex.InnerExceptions.ShouldContain(exception1);
                ex.InnerExceptions.ShouldContain(exception2);
            }
        }

        [Test]
        public async Task SendMessageAsync_EstablishedChannel_SendsToExistingChannel()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            await target.SendMessageAsync(message, CancellationToken.None);
            _establishedClientChannelBuilder.ResetCalls();
            _clientChannel.ResetCalls();

            // Act
            await target.SendMessageAsync(message, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            _clientChannel.Verify(c => c.SendMessageAsync(message, CancellationToken.None), Times.Once());
        }

        [Test]
        public async Task SendMessageAsync_ChannelCreationFailed_RecreateChannelAndSend()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException();

            FailedChannelInformation failedChannelInformation = null;
            target.ChannelCreationFailedHandlers.Add((f) =>
            {                
                failedChannelInformation = f;
                return TaskUtil.TrueCompletedTask;
            });
            ChannelInformation createdChannelInformation = null;
            target.ChannelCreatedHandlers.Add((c) =>
            {                
                createdChannelInformation = c;
                return TaskUtil.CompletedTask;
            });

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendMessageAsync(message, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.SendMessageAsync(message, CancellationToken.None), Times.Once());            
            failedChannelInformation.Exception.ShouldBe(exception);
            failedChannelInformation.IsConnected.ShouldBeFalse();            
            createdChannelInformation.ShouldNotBeNull();
            createdChannelInformation.SessionId.ShouldBe(_sessionId);
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
            var handlerArgs = new List<FailedChannelInformation>();
            target.ChannelCreationFailedHandlers.Add((f) =>
            {                
                handlerArgs.Add(f);
                return TaskUtil.TrueCompletedTask;
            });

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception1)
                .Throws(exception2)
                .Throws(exception3)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendMessageAsync(message, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));
            _clientChannel.Verify(c => c.SendMessageAsync(message, CancellationToken.None), Times.Once());
            handlerArgs.Count.ShouldBe(3);
            handlerArgs.Any(h => h.IsConnected).ShouldBeFalse();
            handlerArgs.Select(e => e.Exception).ShouldContain(exception1);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception2);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception3);
        }

        [Test]
        public async Task SendMessageAsync_ChannelDisposed_ThrowsObjectDisposed()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            target.Dispose();

            // Act
            await target.SendMessageAsync(message, CancellationToken.None).ShouldThrowAsync<ObjectDisposedException>();
        }

        [Test]
        public async Task SendMessageAsync_ChannelCreationFailedHandlerReturnFalse_ThrowsException()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreationFailedHandlers.Add((f) => TaskUtil.FalseCompletedTask);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendMessageAsync(message, CancellationToken.None).ShouldThrowAsync<ApplicationException>();
        }

        [Test]
        public async Task SendMessageAsync_ChannelCreationFailedMultipleHandlersOneReturnsFalse_ThrowsException()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();

            var handlerCallCount = 0;

            target.ChannelCreationFailedHandlers.Add((f) =>
            {
                handlerCallCount++;
                return TaskUtil.TrueCompletedTask;
            });
            target.ChannelCreationFailedHandlers.Add((f) =>
            {
                handlerCallCount++;
                return TaskUtil.FalseCompletedTask;
            });
            target.ChannelCreationFailedHandlers.Add((f) => 
            {
                handlerCallCount++;
                return TaskUtil.TrueCompletedTask;
            });
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act            
            await target.SendMessageAsync(message, CancellationToken.None).ShouldThrowAsync<ApplicationException>();            
            handlerCallCount.ShouldBe(3);            
        }

        [Test]
        public async Task SendMessageAsync_ChannelOperationFailed_RecreateChannelAndSend()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException();
            var sessionId = EnvelopeId.NewId();
            var clientChannel2 = new Mock<IClientChannel>();

            FailedChannelInformation failedChannelInformation = null;
            target.ChannelOperationFailedHandlers.Add((f) =>
            {                
                failedChannelInformation = f;
                return TaskUtil.TrueCompletedTask;
            });
            var createdChannelInformations = new List<ChannelInformation>();
            target.ChannelCreatedHandlers.Add((c) =>
            {                
                createdChannelInformations.Add(c);
                return TaskUtil.CompletedTask;
            });

            ChannelInformation discardedChannelInformation = null;
            target.ChannelDiscardedHandlers.Add((c) =>
            {                                    
                discardedChannelInformation = c;
                return TaskUtil.CompletedTask;
            });
            _clientChannel
                .Setup(c => c.SendMessageAsync(message, CancellationToken.None))
                .Throws(exception);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_clientChannel.Object))
                .Returns(Task.FromResult(clientChannel2.Object));
            clientChannel2
                .SetupGet(c => c.SessionId)
                .Returns(sessionId);
            clientChannel2
                .SetupGet(c => c.Transport)
                .Returns(_transport.Object);
            clientChannel2
                .SetupGet(c => c.State)
                .Returns(SessionState.Established);

            // Act
            await target.SendMessageAsync(message, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.SendMessageAsync(message, CancellationToken.None), Times.Once());
            clientChannel2.Verify(c => c.SendMessageAsync(message, CancellationToken.None), Times.Once());
            failedChannelInformation.Exception.ShouldBe(exception);
            failedChannelInformation.OperationName.ShouldBe(nameof(IOnDemandClientChannel.SendMessageAsync));
            createdChannelInformations.Count.ShouldBe(2);
            createdChannelInformations[0].SessionId.ShouldBe(_sessionId);
            createdChannelInformations[1].SessionId.ShouldBe(sessionId);
            discardedChannelInformation.ShouldNotBeNull();
            discardedChannelInformation.SessionId.ShouldBe(_sessionId);
        }
                        
        [Test]
        public async Task ReceiveMessageAsync_NotEstablishedChannel_BuildChannelAndReceives()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            var target = GetTarget();

            // Act
            var actual = await target.ReceiveMessageAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(message);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            _clientChannel.Verify(c => c.ReceiveMessageAsync(_cancellationToken), Times.Once());            
        }

        [Test]
        public async Task ReceiveMessageAsync_EstablishedChannel_ReceivesFromExistingChannel()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            var target = GetTarget();
            await target.ReceiveMessageAsync(_cancellationToken);
            _establishedClientChannelBuilder.ResetCalls();
            _clientChannel.ResetCalls();

            // Act
            var actual = await target.ReceiveMessageAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(message);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            _clientChannel.Verify(c => c.ReceiveMessageAsync(_cancellationToken), Times.Once());
        }

        [Test]
        public async Task ReceiveMessageAsync_ChannelCreationFailed_RecreateChannelAndReceives()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            var target = GetTarget();
            var exception = Dummy.CreateException();

            FailedChannelInformation failedChannelInformation = null;
            target.ChannelCreationFailedHandlers.Add(f =>
            {    
                failedChannelInformation = f;
                return TaskUtil.TrueCompletedTask;
            });

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveMessageAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(message);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.ReceiveMessageAsync(_cancellationToken), Times.Once());            
            failedChannelInformation.Exception.ShouldBe(exception);
            failedChannelInformation.IsConnected.ShouldBeFalse();
        }

        [Test]
        public async Task ReceiveMessageAsync_ChannelCreationFailsMultipleTimes_TryRecreateChannelAndReceives()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            var target = GetTarget();
            var exception1 = Dummy.CreateException();
            var exception2 = Dummy.CreateException();
            var exception3 = Dummy.CreateException();
            
            var failedChannelInformations = new List<FailedChannelInformation>();
            target.ChannelCreationFailedHandlers.Add(f =>
            {                
                failedChannelInformations.Add(f);
                return TaskUtil.TrueCompletedTask;
            });

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception1)
                .Throws(exception2)
                .Throws(exception3)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveMessageAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(message);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));
            _clientChannel.Verify(c => c.ReceiveMessageAsync(_cancellationToken), Times.Once());
            failedChannelInformations.Count.ShouldBe(3);
            failedChannelInformations.Any(h => h.IsConnected).ShouldBeFalse();
            failedChannelInformations.Select(e => e.Exception).ShouldContain(exception1);
            failedChannelInformations.Select(e => e.Exception).ShouldContain(exception2);
            failedChannelInformations.Select(e => e.Exception).ShouldContain(exception3);
        }

        [Test]
        public async Task ReceiveMessageAsync_ChannelDisposed_ThrowsObjectDisposed()
        {
            // Arrange            
            var target = GetTarget();
            target.Dispose();

            // Act
            var actual = await target.ReceiveMessageAsync(_cancellationToken).ShouldThrowAsync<ObjectDisposedException>();
        }

        [Test]
        public void ReceiveMessageAsync_CanceledToken_ThrowsTaskCanceledException()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            var target = GetTarget();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            target.ReceiveMessageAsync(cts.Token).ShouldThrow<TaskCanceledException>();
        }

        [Test]
        public async Task ReceiveMessageAsync_ChannelCreationFailedHandlerReturnFalse_ThrowsException()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreationFailedHandlers.Add(f => TaskUtil.FalseCompletedTask);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveMessageAsync(_cancellationToken).ShouldThrowAsync<ApplicationException>();
        }

        [Test]
        public async Task ReceiveMessageAsync_ChannelOperationFailed_RecreateChannelAndReceives()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            var target = GetTarget();
            var exception = Dummy.CreateException();
            var clientChannel2 = new Mock<IClientChannel>();
            
            FailedChannelInformation failedChannelInformation = null;
            target.ChannelOperationFailedHandlers.Add(f =>
            {
                failedChannelInformation = f;
                return TaskUtil.TrueCompletedTask;
            });
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Throws(exception);
            clientChannel2
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_clientChannel.Object))
                .Returns(Task.FromResult(clientChannel2.Object));

            // Act
            var actual = await target.ReceiveMessageAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(message);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.ReceiveMessageAsync(_cancellationToken), Times.Once());
            _disposableClientChannel.Verify(c => c.Dispose(), Times.Once);
            clientChannel2.Verify(c => c.ReceiveMessageAsync(_cancellationToken), Times.Once());            
            failedChannelInformation.Exception.ShouldBe(exception);
            failedChannelInformation.OperationName.ShouldBe(nameof(IOnDemandClientChannel.ReceiveMessageAsync));
        }
        
        [Test]
        public async Task SendNotificationAsync_NotEstablishedChannel_BuildChannelAndSends()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var target = GetTarget();
            ChannelInformation channelInformation = null;
            target.ChannelCreatedHandlers.Add((c) =>
            {
                channelInformation = c;
                return TaskUtil.CompletedTask;
            });

            // Act
            await target.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            _clientChannel.Verify(c => c.SendNotificationAsync(notification, CancellationToken.None), Times.Once());
            channelInformation.ShouldNotBeNull();
            channelInformation.SessionId.ShouldBe(_sessionId);
            channelInformation.State.ShouldBe(SessionState.Established);
        }

        [Test]
        public async Task SendNotificationAsync_NotEstablishedChannelMultipleCalls_BuildChannelOnceAndSends()
        {
            // Arrange
            var count = Dummy.CreateRandomInt(500) + 1; ;
            var notifications = new Notification[count];
            for (int i = 0; i < count; i++)
            {
                notifications[i] = Dummy.CreateNotification(Event.Received);
            }

            var target = GetTarget();

            // Act
            await Task.WhenAll(
                Enumerable
                    .Range(0, count)
                    .Select(i => Task.Run(() => target.SendNotificationAsync(notifications[i], CancellationToken.None))));


            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            foreach (var notification in notifications)
            {
                _clientChannel.Verify(c => c.SendNotificationAsync(notification, CancellationToken.None), Times.Once());
            }
        }

        [Test]
        public async Task SendNotificationAsync_ChannelCreatedHandlerThrowsException_ThrowsExceptionToTheCaller()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreatedHandlers.Add((c) =>
            {
                throw exception;
            });

            // Act
            await target.SendNotificationAsync(notification, CancellationToken.None).ShouldThrowAsync<ApplicationException>();
        }

        [Test]
        public async Task SendNotificationAsync_MultipleChannelCreatedHandlerThrowsException_ThrowsAggregateExceptionToTheCaller()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var target = GetTarget();
            var exception1 = Dummy.CreateException<ApplicationException>();
            target.ChannelCreatedHandlers.Add((c) =>
            {
                throw exception1;
            });
            var exception2 = Dummy.CreateException<ApplicationException>();
            target.ChannelCreatedHandlers.Add((c) =>
            {
                throw exception2;
            });

            // Act
            try
            {
                await target.SendNotificationAsync(notification, CancellationToken.None);
            }
            catch (AggregateException ex)
            {
                ex.InnerExceptions.Count.ShouldBe(2);
                ex.InnerExceptions.ShouldContain(exception1);
                ex.InnerExceptions.ShouldContain(exception2);
            }
        }

        [Test]
        public async Task SendNotificationAsync_EstablishedChannel_SendsToExistingChannel()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var target = GetTarget();
            await target.SendNotificationAsync(notification, CancellationToken.None);
            _establishedClientChannelBuilder.ResetCalls();
            _clientChannel.ResetCalls();

            // Act
            await target.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            _clientChannel.Verify(c => c.SendNotificationAsync(notification, CancellationToken.None), Times.Once());
        }

        [Test]
        public async Task SendNotificationAsync_ChannelCreationFailed_RecreateChannelAndSend()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var target = GetTarget();
            var exception = Dummy.CreateException();

            FailedChannelInformation failedChannelInformation = null;
            target.ChannelCreationFailedHandlers.Add((f) =>
            {
                failedChannelInformation = f;
                return TaskUtil.TrueCompletedTask;
            });
            ChannelInformation createdChannelInformation = null;
            target.ChannelCreatedHandlers.Add((c) =>
            {
                createdChannelInformation = c;
                return TaskUtil.CompletedTask;
            });

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.SendNotificationAsync(notification, CancellationToken.None), Times.Once());
            failedChannelInformation.Exception.ShouldBe(exception);
            failedChannelInformation.IsConnected.ShouldBeFalse();
            createdChannelInformation.ShouldNotBeNull();
            createdChannelInformation.SessionId.ShouldBe(_sessionId);
        }

        [Test]
        public async Task SendNotificationAsync_ChannelCreationFailsMultipleTimes_TryRecreateChannelAndSend()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var target = GetTarget();
            var exception1 = Dummy.CreateException();
            var exception2 = Dummy.CreateException();
            var exception3 = Dummy.CreateException();
            var handlerArgs = new List<FailedChannelInformation>();
            target.ChannelCreationFailedHandlers.Add((f) =>
            {
                handlerArgs.Add(f);
                return TaskUtil.TrueCompletedTask;
            });

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception1)
                .Throws(exception2)
                .Throws(exception3)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));
            _clientChannel.Verify(c => c.SendNotificationAsync(notification, CancellationToken.None), Times.Once());
            handlerArgs.Count.ShouldBe(3);
            handlerArgs.Any(h => h.IsConnected).ShouldBeFalse();
            handlerArgs.Select(e => e.Exception).ShouldContain(exception1);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception2);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception3);
        }

        [Test]
        public async Task SendNotificationAsync_ChannelDisposed_ThrowsObjectDisposed()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var target = GetTarget();
            target.Dispose();

            // Act
            await target.SendNotificationAsync(notification, CancellationToken.None).ShouldThrowAsync<ObjectDisposedException>();
        }

        [Test]
        public async Task SendNotificationAsync_ChannelCreationFailedHandlerReturnFalse_ThrowsException()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreationFailedHandlers.Add((f) => TaskUtil.FalseCompletedTask);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendNotificationAsync(notification, CancellationToken.None).ShouldThrowAsync<ApplicationException>();
        }

        [Test]
        public async Task SendNotificationAsync_ChannelCreationFailedMultipleHandlersOneReturnsFalse_ThrowsException()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();

            var handlerCallCount = 0;

            target.ChannelCreationFailedHandlers.Add((f) =>
            {
                handlerCallCount++;
                return TaskUtil.TrueCompletedTask;
            });
            target.ChannelCreationFailedHandlers.Add((f) =>
            {
                handlerCallCount++;
                return TaskUtil.FalseCompletedTask;
            });
            target.ChannelCreationFailedHandlers.Add((f) =>
            {
                handlerCallCount++;
                return TaskUtil.TrueCompletedTask;
            });
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act            
            await target.SendNotificationAsync(notification, CancellationToken.None).ShouldThrowAsync<ApplicationException>();
            handlerCallCount.ShouldBe(3);
        }

        [Test]
        public async Task SendNotificationAsync_ChannelOperationFailed_RecreateChannelAndSend()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var target = GetTarget();
            var exception = Dummy.CreateException();
            var sessionId = EnvelopeId.NewId();
            var clientChannel2 = new Mock<IClientChannel>();

            FailedChannelInformation failedChannelInformation = null;
            target.ChannelOperationFailedHandlers.Add((f) =>
            {
                failedChannelInformation = f;
                return TaskUtil.TrueCompletedTask;
            });
            var createdChannelInformations = new List<ChannelInformation>();
            target.ChannelCreatedHandlers.Add((c) =>
            {
                createdChannelInformations.Add(c);
                return TaskUtil.CompletedTask;
            });

            ChannelInformation discardedChannelInformation = null;
            target.ChannelDiscardedHandlers.Add((c) =>
            {
                discardedChannelInformation = c;
                return TaskUtil.CompletedTask;
            });
            _clientChannel
                .Setup(c => c.SendNotificationAsync(notification, CancellationToken.None))
                .Throws(exception);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_clientChannel.Object))
                .Returns(Task.FromResult(clientChannel2.Object));
            clientChannel2
                .SetupGet(c => c.SessionId)
                .Returns(sessionId);
            clientChannel2
                .SetupGet(c => c.Transport)
                .Returns(_transport.Object);
            clientChannel2
                .SetupGet(c => c.State)
                .Returns(SessionState.Established);

            // Act
            await target.SendNotificationAsync(notification, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.SendNotificationAsync(notification, CancellationToken.None), Times.Once());
            clientChannel2.Verify(c => c.SendNotificationAsync(notification, CancellationToken.None), Times.Once());
            failedChannelInformation.Exception.ShouldBe(exception);
            failedChannelInformation.OperationName.ShouldBe(nameof(IOnDemandClientChannel.SendNotificationAsync));
            createdChannelInformations.Count.ShouldBe(2);
            createdChannelInformations[0].SessionId.ShouldBe(_sessionId);
            createdChannelInformations[1].SessionId.ShouldBe(sessionId);
            discardedChannelInformation.ShouldNotBeNull();
            discardedChannelInformation.SessionId.ShouldBe(_sessionId);
        }

        [Test]
        public async Task ReceiveNotificationAsync_NotEstablishedChannel_BuildChannelAndReceives()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);
            var target = GetTarget();

            // Act
            var actual = await target.ReceiveNotificationAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(notification);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            _clientChannel.Verify(c => c.ReceiveNotificationAsync(_cancellationToken), Times.Once());
        }

        [Test]
        public async Task ReceiveNotificationAsync_EstablishedChannel_ReceivesFromExistingChannel()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);
            var target = GetTarget();
            await target.ReceiveNotificationAsync(_cancellationToken);
            _establishedClientChannelBuilder.ResetCalls();
            _clientChannel.ResetCalls();

            // Act
            var actual = await target.ReceiveNotificationAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(notification);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            _clientChannel.Verify(c => c.ReceiveNotificationAsync(_cancellationToken), Times.Once());
        }

        [Test]
        public async Task ReceiveNotificationAsync_ChannelCreationFailed_RecreateChannelAndReceives()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);
            var target = GetTarget();
            var exception = Dummy.CreateException();

            FailedChannelInformation failedChannelInformation = null;
            target.ChannelCreationFailedHandlers.Add(f =>
            {
                failedChannelInformation = f;
                return TaskUtil.TrueCompletedTask;
            });

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveNotificationAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(notification);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.ReceiveNotificationAsync(_cancellationToken), Times.Once());
            failedChannelInformation.Exception.ShouldBe(exception);
            failedChannelInformation.IsConnected.ShouldBeFalse();
        }

        [Test]
        public async Task ReceiveNotificationAsync_ChannelCreationFailsMultipleTimes_TryRecreateChannelAndReceives()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);
            var target = GetTarget();
            var exception1 = Dummy.CreateException();
            var exception2 = Dummy.CreateException();
            var exception3 = Dummy.CreateException();

            var failedChannelInformations = new List<FailedChannelInformation>();
            target.ChannelCreationFailedHandlers.Add(f =>
            {
                failedChannelInformations.Add(f);
                return TaskUtil.TrueCompletedTask;
            });

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception1)
                .Throws(exception2)
                .Throws(exception3)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveNotificationAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(notification);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));
            _clientChannel.Verify(c => c.ReceiveNotificationAsync(_cancellationToken), Times.Once());
            failedChannelInformations.Count.ShouldBe(3);
            failedChannelInformations.Any(h => h.IsConnected).ShouldBeFalse();
            failedChannelInformations.Select(e => e.Exception).ShouldContain(exception1);
            failedChannelInformations.Select(e => e.Exception).ShouldContain(exception2);
            failedChannelInformations.Select(e => e.Exception).ShouldContain(exception3);
        }

        [Test]
        public async Task ReceiveNotificationAsync_ChannelDisposed_ThrowsObjectDisposed()
        {
            // Arrange            
            var target = GetTarget();
            target.Dispose();

            // Act
            var actual = await target.ReceiveNotificationAsync(_cancellationToken).ShouldThrowAsync<ObjectDisposedException>();
        }

        [Test]
        public void ReceiveNotificationAsync_CanceledToken_ThrowsTaskCanceledException()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);
            var target = GetTarget();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            target.ReceiveNotificationAsync(cts.Token).ShouldThrow<TaskCanceledException>();
        }

        [Test]
        public async Task ReceiveNotificationAsync_ChannelCreationFailedHandlerReturnFalse_ThrowsException()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreationFailedHandlers.Add(f => TaskUtil.FalseCompletedTask);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveNotificationAsync(_cancellationToken).ShouldThrowAsync<ApplicationException>();
        }

        [Test]
        public async Task ReceiveNotificationAsync_ChannelOperationFailed_RecreateChannelAndReceives()
        {
            // Arrange
            var notification = Dummy.CreateNotification(Event.Received);
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);
            var target = GetTarget();
            var exception = Dummy.CreateException();
            var clientChannel2 = new Mock<IClientChannel>();

            FailedChannelInformation failedChannelInformation = null;
            target.ChannelOperationFailedHandlers.Add(f =>
            {
                failedChannelInformation = f;
                return TaskUtil.TrueCompletedTask;
            });
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .Throws(exception);
            clientChannel2
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_clientChannel.Object))
                .Returns(Task.FromResult(clientChannel2.Object));

            // Act
            var actual = await target.ReceiveNotificationAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(notification);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.ReceiveNotificationAsync(_cancellationToken), Times.Once());
            _disposableClientChannel.Verify(c => c.Dispose(), Times.Once);
            clientChannel2.Verify(c => c.ReceiveNotificationAsync(_cancellationToken), Times.Once());
            failedChannelInformation.Exception.ShouldBe(exception);
            failedChannelInformation.OperationName.ShouldBe(nameof(IOnDemandClientChannel.ReceiveNotificationAsync));
        }
        
        [Test]
        public async Task SendCommandAsync_NotEstablishedChannel_BuildChannelAndSends()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            ChannelInformation channelInformation = null;
            target.ChannelCreatedHandlers.Add((c) =>
            {
                channelInformation = c;
                return TaskUtil.CompletedTask;
            });

            // Act
            await target.SendCommandAsync(command, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            _clientChannel.Verify(c => c.SendCommandAsync(command, CancellationToken.None), Times.Once());
            channelInformation.ShouldNotBeNull();
            channelInformation.SessionId.ShouldBe(_sessionId);
            channelInformation.State.ShouldBe(SessionState.Established);
        }

        [Test]
        public async Task SendCommandAsync_NotEstablishedChannelMultipleCalls_BuildChannelOnceAndSends()
        {
            // Arrange
            var count = Dummy.CreateRandomInt(500) + 1; ;
            var commands = new Command[count];
            for (int i = 0; i < count; i++)
            {
                commands[i] = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            }

            var target = GetTarget();

            // Act
            await Task.WhenAll(
                Enumerable
                    .Range(0, count)
                    .Select(i => Task.Run(() => target.SendCommandAsync(commands[i], CancellationToken.None))));


            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            foreach (var command in commands)
            {
                _clientChannel.Verify(c => c.SendCommandAsync(command, CancellationToken.None), Times.Once());
            }
        }

        [Test]
        public async Task SendCommandAsync_ChannelCreatedHandlerThrowsException_ThrowsExceptionToTheCaller()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreatedHandlers.Add((c) =>
            {
                throw exception;
            });

            // Act
            await target.SendCommandAsync(command, CancellationToken.None).ShouldThrowAsync<ApplicationException>();
        }

        [Test]
        public async Task SendCommandAsync_MultipleChannelCreatedHandlerThrowsException_ThrowsAggregateExceptionToTheCaller()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception1 = Dummy.CreateException<ApplicationException>();
            target.ChannelCreatedHandlers.Add((c) =>
            {
                throw exception1;
            });
            var exception2 = Dummy.CreateException<ApplicationException>();
            target.ChannelCreatedHandlers.Add((c) =>
            {
                throw exception2;
            });

            // Act
            try
            {
                await target.SendCommandAsync(command, CancellationToken.None);
            }
            catch (AggregateException ex)
            {
                ex.InnerExceptions.Count.ShouldBe(2);
                ex.InnerExceptions.ShouldContain(exception1);
                ex.InnerExceptions.ShouldContain(exception2);
            }
        }

        [Test]
        public async Task SendCommandAsync_EstablishedChannel_SendsToExistingChannel()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            await target.SendCommandAsync(command, CancellationToken.None);
            _establishedClientChannelBuilder.ResetCalls();
            _clientChannel.ResetCalls();

            // Act
            await target.SendCommandAsync(command, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            _clientChannel.Verify(c => c.SendCommandAsync(command, CancellationToken.None), Times.Once());
        }

        [Test]
        public async Task SendCommandAsync_ChannelCreationFailed_RecreateChannelAndSend()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException();

            FailedChannelInformation failedChannelInformation = null;
            target.ChannelCreationFailedHandlers.Add((f) =>
            {
                failedChannelInformation = f;
                return TaskUtil.TrueCompletedTask;
            });
            ChannelInformation createdChannelInformation = null;
            target.ChannelCreatedHandlers.Add((c) =>
            {
                createdChannelInformation = c;
                return TaskUtil.CompletedTask;
            });

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendCommandAsync(command, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.SendCommandAsync(command, CancellationToken.None), Times.Once());
            failedChannelInformation.Exception.ShouldBe(exception);
            failedChannelInformation.IsConnected.ShouldBeFalse();
            createdChannelInformation.ShouldNotBeNull();
            createdChannelInformation.SessionId.ShouldBe(_sessionId);
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
            var handlerArgs = new List<FailedChannelInformation>();
            target.ChannelCreationFailedHandlers.Add((f) =>
            {
                handlerArgs.Add(f);
                return TaskUtil.TrueCompletedTask;
            });

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception1)
                .Throws(exception2)
                .Throws(exception3)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendCommandAsync(command, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));
            _clientChannel.Verify(c => c.SendCommandAsync(command, CancellationToken.None), Times.Once());
            handlerArgs.Count.ShouldBe(3);
            handlerArgs.Any(h => h.IsConnected).ShouldBeFalse();
            handlerArgs.Select(e => e.Exception).ShouldContain(exception1);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception2);
            handlerArgs.Select(e => e.Exception).ShouldContain(exception3);
        }

        [Test]
        public async Task SendCommandAsync_ChannelDisposed_ThrowsObjectDisposed()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            target.Dispose();

            // Act
            await target.SendCommandAsync(command, CancellationToken.None).ShouldThrowAsync<ObjectDisposedException>();
        }

        [Test]
        public async Task SendCommandAsync_ChannelCreationFailedHandlerReturnFalse_ThrowsException()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreationFailedHandlers.Add((f) => TaskUtil.FalseCompletedTask);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            await target.SendCommandAsync(command, CancellationToken.None).ShouldThrowAsync<ApplicationException>();
        }

        [Test]
        public async Task SendCommandAsync_ChannelCreationFailedMultipleHandlersOneReturnsFalse_ThrowsException()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();

            var handlerCallCount = 0;

            target.ChannelCreationFailedHandlers.Add((f) =>
            {
                handlerCallCount++;
                return TaskUtil.TrueCompletedTask;
            });
            target.ChannelCreationFailedHandlers.Add((f) =>
            {
                handlerCallCount++;
                return TaskUtil.FalseCompletedTask;
            });
            target.ChannelCreationFailedHandlers.Add((f) =>
            {
                handlerCallCount++;
                return TaskUtil.TrueCompletedTask;
            });
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act            
            await target.SendCommandAsync(command, CancellationToken.None).ShouldThrowAsync<ApplicationException>();
            handlerCallCount.ShouldBe(3);
        }

        [Test]
        public async Task SendCommandAsync_ChannelOperationFailed_RecreateChannelAndSend()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException();
            var sessionId = EnvelopeId.NewId();
            var clientChannel2 = new Mock<IClientChannel>();

            FailedChannelInformation failedChannelInformation = null;
            target.ChannelOperationFailedHandlers.Add((f) =>
            {
                failedChannelInformation = f;
                return TaskUtil.TrueCompletedTask;
            });
            var createdChannelInformations = new List<ChannelInformation>();
            target.ChannelCreatedHandlers.Add((c) =>
            {
                createdChannelInformations.Add(c);
                return TaskUtil.CompletedTask;
            });

            ChannelInformation discardedChannelInformation = null;
            target.ChannelDiscardedHandlers.Add((c) =>
            {
                discardedChannelInformation = c;
                return TaskUtil.CompletedTask;
            });
            _clientChannel
                .Setup(c => c.SendCommandAsync(command, CancellationToken.None))
                .Throws(exception);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_clientChannel.Object))
                .Returns(Task.FromResult(clientChannel2.Object));
            clientChannel2
                .SetupGet(c => c.SessionId)
                .Returns(sessionId);
            clientChannel2
                .SetupGet(c => c.Transport)
                .Returns(_transport.Object);
            clientChannel2
                .SetupGet(c => c.State)
                .Returns(SessionState.Established);

            // Act
            await target.SendCommandAsync(command, CancellationToken.None);

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.SendCommandAsync(command, CancellationToken.None), Times.Once());
            clientChannel2.Verify(c => c.SendCommandAsync(command, CancellationToken.None), Times.Once());
            failedChannelInformation.OperationName.ShouldBe(nameof(IOnDemandClientChannel.SendCommandAsync));
            failedChannelInformation.Exception.ShouldBe(exception);
            failedChannelInformation.OperationName.ShouldBe(nameof(IOnDemandClientChannel.SendCommandAsync));
            createdChannelInformations.Count.ShouldBe(2);
            createdChannelInformations[0].SessionId.ShouldBe(_sessionId);
            createdChannelInformations[1].SessionId.ShouldBe(sessionId);
            discardedChannelInformation.ShouldNotBeNull();
            discardedChannelInformation.SessionId.ShouldBe(_sessionId);
        }

        [Test]
        public async Task ReceiveCommandAsync_NotEstablishedChannel_BuildChannelAndReceives()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(command);
            var target = GetTarget();

            // Act
            var actual = await target.ReceiveCommandAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(command);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            _clientChannel.Verify(c => c.ReceiveCommandAsync(_cancellationToken), Times.Once());
        }

        [Test]
        public async Task ReceiveCommandAsync_EstablishedChannel_ReceivesFromExistingChannel()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(command);
            var target = GetTarget();
            await target.ReceiveCommandAsync(_cancellationToken);
            _establishedClientChannelBuilder.ResetCalls();
            _clientChannel.ResetCalls();

            // Act
            var actual = await target.ReceiveCommandAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(command);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            _clientChannel.Verify(c => c.ReceiveCommandAsync(_cancellationToken), Times.Once());
        }

        [Test]
        public async Task ReceiveCommandAsync_ChannelCreationFailed_RecreateChannelAndReceives()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(command);
            var target = GetTarget();
            var exception = Dummy.CreateException();

            FailedChannelInformation failedChannelInformation = null;
            target.ChannelCreationFailedHandlers.Add(f =>
            {
                failedChannelInformation = f;
                return TaskUtil.TrueCompletedTask;
            });

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveCommandAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(command);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.ReceiveCommandAsync(_cancellationToken), Times.Once());
            failedChannelInformation.Exception.ShouldBe(exception);
            failedChannelInformation.IsConnected.ShouldBeFalse();
        }

        [Test]
        public async Task ReceiveCommandAsync_ChannelCreationFailsMultipleTimes_TryRecreateChannelAndReceives()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(command);
            var target = GetTarget();
            var exception1 = Dummy.CreateException();
            var exception2 = Dummy.CreateException();
            var exception3 = Dummy.CreateException();

            var failedChannelInformations = new List<FailedChannelInformation>();
            target.ChannelCreationFailedHandlers.Add(f =>
            {
                failedChannelInformations.Add(f);
                return TaskUtil.TrueCompletedTask;
            });

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception1)
                .Throws(exception2)
                .Throws(exception3)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveCommandAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(command);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));
            _clientChannel.Verify(c => c.ReceiveCommandAsync(_cancellationToken), Times.Once());
            failedChannelInformations.Count.ShouldBe(3);
            failedChannelInformations.Any(h => h.IsConnected).ShouldBeFalse();
            failedChannelInformations.Select(e => e.Exception).ShouldContain(exception1);
            failedChannelInformations.Select(e => e.Exception).ShouldContain(exception2);
            failedChannelInformations.Select(e => e.Exception).ShouldContain(exception3);
        }

        [Test]
        public async Task ReceiveCommandAsync_ChannelDisposed_ThrowsObjectDisposed()
        {
            // Arrange            
            var target = GetTarget();
            target.Dispose();

            // Act
            var actual = await target.ReceiveCommandAsync(_cancellationToken).ShouldThrowAsync<ObjectDisposedException>();
        }

        [Test]
        public async Task ReceiveCommandAsync_CanceledToken_ThrowsTaskCanceledException()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(command);
            var target = GetTarget();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            target.ReceiveCommandAsync(cts.Token).ShouldThrow<TaskCanceledException>();
        }

        [Test]
        public async Task ReceiveCommandAsync_ChannelCreationFailedHandlerReturnFalse_ThrowsException()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreationFailedHandlers.Add(f => TaskUtil.FalseCompletedTask);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ReceiveCommandAsync(_cancellationToken).ShouldThrowAsync<ApplicationException>();
        }

        [Test]
        public async Task ReceiveCommandAsync_ChannelOperationFailed_RecreateChannelAndReceives()
        {
            // Arrange
            var command = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(command);
            var target = GetTarget();
            var exception = Dummy.CreateException();
            var clientChannel2 = new Mock<IClientChannel>();

            FailedChannelInformation failedChannelInformation = null;
            target.ChannelOperationFailedHandlers.Add(f =>
            {
                failedChannelInformation = f;
                return TaskUtil.TrueCompletedTask;
            });
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .Throws(exception);
            clientChannel2
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(command);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_clientChannel.Object))
                .Returns(Task.FromResult(clientChannel2.Object));

            // Act
            var actual = await target.ReceiveCommandAsync(_cancellationToken);

            // Assert
            actual.ShouldBe(command);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.ReceiveCommandAsync(_cancellationToken), Times.Once());
            _disposableClientChannel.Verify(c => c.Dispose(), Times.Once);
            clientChannel2.Verify(c => c.ReceiveCommandAsync(_cancellationToken), Times.Once());
            failedChannelInformation.Exception.ShouldBe(exception);
            failedChannelInformation.OperationName.ShouldBe(nameof(IOnDemandClientChannel.ReceiveCommandAsync));
        }

        
        [Test]
        public async Task ProcessCommandAsync_NotEstablishedChannel_BuildChannelAndProcesses()
        {
            // Arrange
            var requestCommand = Dummy.CreateCommand();
            var responseCommand = Dummy.CreateCommand(Dummy.CreatePing(), status: CommandStatus.Success);
            responseCommand.Id = requestCommand.Id;
            _clientChannel
                .Setup(c => c.ProcessCommandAsync(It.IsAny<Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseCommand);
            var target = GetTarget();

            // Act
            var actual = await target.ProcessCommandAsync(requestCommand, _cancellationToken);

            // Assert
            actual.ShouldBe(responseCommand);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Once());
            _clientChannel.Verify(c => c.ProcessCommandAsync(requestCommand, _cancellationToken), Times.Once());
        }

        [Test]
        public async Task ProcessCommandAsync_EstablishedChannel_ProcessesWithExistingChannel()
        {
            // Arrange
            var requestCommand = Dummy.CreateCommand();
            var responseCommand = Dummy.CreateCommand(Dummy.CreatePing(), status: CommandStatus.Success);
            responseCommand.Id = requestCommand.Id;
            _clientChannel
                .Setup(c => c.ProcessCommandAsync(It.IsAny<Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseCommand);
            var target = GetTarget();
            await target.ProcessCommandAsync(requestCommand, _cancellationToken);
            _establishedClientChannelBuilder.ResetCalls();
            _clientChannel.ResetCalls();

            // Act
            var actual = await target.ProcessCommandAsync(requestCommand, _cancellationToken);

            // Assert
            actual.ShouldBe(responseCommand);      
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Never());
            _clientChannel.Verify(c => c.ProcessCommandAsync(requestCommand, _cancellationToken), Times.Once());
        }

        [Test]
        public async Task ProcessCommandAsync_ChannelCreationFailed_RecreateChannelAndProcesses()
        {
            // Arrange
            var requestCommand = Dummy.CreateCommand();
            var responseCommand = Dummy.CreateCommand(Dummy.CreatePing(), status: CommandStatus.Success);
            responseCommand.Id = requestCommand.Id;
            _clientChannel
                .Setup(c => c.ProcessCommandAsync(It.IsAny<Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseCommand);
            var target = GetTarget();
            var exception = Dummy.CreateException();

            FailedChannelInformation failedChannelInformation = null;
            target.ChannelCreationFailedHandlers.Add(f =>
            {
                failedChannelInformation = f;
                return TaskUtil.TrueCompletedTask;
            });

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ProcessCommandAsync(requestCommand, _cancellationToken);

            // Assert
            actual.ShouldBe(responseCommand);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.ProcessCommandAsync(requestCommand, _cancellationToken), Times.Once());
            failedChannelInformation.Exception.ShouldBe(exception);
            failedChannelInformation.IsConnected.ShouldBeFalse();
        }

        [Test]
        public async Task ProcessCommandAsync_ChannelCreationFailsMultipleTimes_TryRecreateChannelAndProcesses()
        {
            // Arrange
            var requestCommand = Dummy.CreateCommand();
            var responseCommand = Dummy.CreateCommand(Dummy.CreatePing(), status: CommandStatus.Success);
            responseCommand.Id = requestCommand.Id;
            _clientChannel
                .Setup(c => c.ProcessCommandAsync(It.IsAny<Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseCommand);
            var target = GetTarget();
            var exception1 = Dummy.CreateException();
            var exception2 = Dummy.CreateException();
            var exception3 = Dummy.CreateException();

            var failedChannelInformations = new List<FailedChannelInformation>();
            target.ChannelCreationFailedHandlers.Add(f =>
            {
                failedChannelInformations.Add(f);
                return TaskUtil.TrueCompletedTask;
            });

            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception1)
                .Throws(exception2)
                .Throws(exception3)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ProcessCommandAsync(requestCommand, _cancellationToken);

            // Assert
            actual.ShouldBe(responseCommand);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(4));
            _clientChannel.Verify(c => c.ProcessCommandAsync(requestCommand, _cancellationToken), Times.Once());
            failedChannelInformations.Count.ShouldBe(3);
            failedChannelInformations.Any(h => h.IsConnected).ShouldBeFalse();
            failedChannelInformations.Select(e => e.Exception).ShouldContain(exception1);
            failedChannelInformations.Select(e => e.Exception).ShouldContain(exception2);
            failedChannelInformations.Select(e => e.Exception).ShouldContain(exception3);
        }

        [Test]
        public async Task ProcessCommandAsync_ChannelDisposed_ThrowsObjectDisposed()
        {
            // Arrange            
            var requestCommand = Dummy.CreateCommand();
            var target = GetTarget();
            target.Dispose();

            // Act
            var actual = await target.ProcessCommandAsync(requestCommand, _cancellationToken).ShouldThrowAsync<ObjectDisposedException>();
        }

        [Test]
        public void ProcessCommandAsync_CanceledToken_ThrowsTaskCanceledException()
        {
            // Arrange
            var requestCommand = Dummy.CreateCommand();
            var responseCommand = Dummy.CreateCommand(Dummy.CreatePing(), status: CommandStatus.Success);
            responseCommand.Id = requestCommand.Id;
            _clientChannel
                .Setup(c => c.ProcessCommandAsync(It.IsAny<Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseCommand);
            var target = GetTarget();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act            
            var task = target.ProcessCommandAsync(requestCommand, cts.Token);
            task.ShouldThrow<TaskCanceledException>();
        }

        [Test]        
        public async Task ProcessCommandAsync_ChannelCreationFailedHandlerReturnFalse_ThrowsException()
        {
            // Arrange
            var requestCommand = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var target = GetTarget();
            var exception = Dummy.CreateException<ApplicationException>();
            target.ChannelCreationFailedHandlers.Add(f => TaskUtil.FalseCompletedTask);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Throws(exception)
                .Returns(Task.FromResult(_clientChannel.Object));

            // Act
            var actual = await target.ProcessCommandAsync(requestCommand, _cancellationToken).ShouldThrowAsync<ApplicationException>();
        }

        [Test]
        public async Task ProcessCommandAsync_ChannelOperationFailed_RecreateChannelAndProcesses()
        {
            // Arrange
            var requestCommand = Dummy.CreateCommand();
            var responseCommand = Dummy.CreateCommand(Dummy.CreatePing(), status: CommandStatus.Success);
            var sessionId = EnvelopeId.NewId();
            responseCommand.Id = requestCommand.Id;
            var target = GetTarget();
            var exception = Dummy.CreateException();
            var clientChannel2 = new Mock<IClientChannel>();

            FailedChannelInformation failedChannelInformation = null;
            target.ChannelOperationFailedHandlers.Add(f =>
            {
                failedChannelInformation = f;
                return TaskUtil.TrueCompletedTask;
            });
            _clientChannel
                .Setup(c => c.ProcessCommandAsync(It.IsAny<Command>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            clientChannel2
                .Setup(c => c.ProcessCommandAsync(It.IsAny<Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseCommand);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_clientChannel.Object))
                .Returns(Task.FromResult(clientChannel2.Object));
            clientChannel2
                .SetupGet(c => c.SessionId)
                .Returns(sessionId);
            clientChannel2
                .SetupGet(c => c.Transport)
                .Returns(_transport.Object);
            clientChannel2
                .SetupGet(c => c.State)
                .Returns(SessionState.Established);

            // Act
            var actual = await target.ProcessCommandAsync(requestCommand, _cancellationToken);

            // Assert
            actual.ShouldBe(responseCommand);
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannel.Verify(c => c.ProcessCommandAsync(requestCommand, _cancellationToken), Times.Once());
            _disposableClientChannel.Verify(c => c.Dispose(), Times.Once);
            clientChannel2.Verify(c => c.ProcessCommandAsync(requestCommand, _cancellationToken), Times.Once());
            failedChannelInformation.Exception.ShouldBe(exception);
            failedChannelInformation.OperationName.ShouldBe(nameof(IOnDemandClientChannel.ProcessCommandAsync));
        }

        [Test]
        public async Task FinishAsync_EstablishedChannel_SendFinishingAndAwaitsForFinishedSession()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            await target.SendMessageAsync(message, CancellationToken.None);
            var session = Dummy.CreateSession(SessionState.Finished);
            _clientChannel
                .Setup(c => c.ReceiveFinishedSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);                

            // Act
            await target.FinishAsync(_cancellationToken);

            // Assert
            _clientChannel.Verify(c => c.SendFinishingSessionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _clientChannel.Verify(c => c.ReceiveFinishedSessionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _disposableClientChannel.Verify(c => c.Dispose(), Times.Once);
        }

        [Test]
        public async Task FinishAsync_NotEstablishedChannel_DoNotSendEnvelopes()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var target = GetTarget();
            await target.SendMessageAsync(message, CancellationToken.None);
            var session = Dummy.CreateSession(SessionState.Finished);
            _establishedClientChannelBuilder.ResetCalls();
            _clientChannel.ResetCalls();
            _clientChannel
                .SetupGet(c => c.State)
                .Returns(SessionState.Finished);

            // Act
            await target.FinishAsync(_cancellationToken);

            // Assert
            _clientChannel.Verify(c => c.SendFinishingSessionAsync(CancellationToken.None), Times.Never);
            _clientChannel.Verify(c => c.ReceiveFinishedSessionAsync(_cancellationToken), Times.Never);
            _disposableClientChannel.Verify(c => c.Dispose(), Times.Once);
        }
        
        [Test]
        public async Task Send_Receive_Process_Message_Command_Notification_Async_ChannelOperationFailed_RecreateChannelOnceAndSend()
        {
            // Arrange
            var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
            var receivedMessage = Dummy.CreateMessage(Dummy.CreateJsonDocument());
            var notification = Dummy.CreateNotification(Event.Received);
            var receivedNotification = Dummy.CreateNotification(Event.Accepted);
            var command = Dummy.CreateCommand(Dummy.CreatePing());
            var receivedCommand = Dummy.CreateCommand(Dummy.CreatePlainDocument());
            var requestCommand = Dummy.CreateCommand();
            var responseCommand = Dummy.CreateCommand(Dummy.CreatePing(), status: CommandStatus.Success);

            var target = GetTarget();
            var exception = Dummy.CreateException();
            var sessionId = EnvelopeId.NewId();
            var clientChannel2 = new Mock<IClientChannel>();
            var clientChannel2Disposable = clientChannel2.As<IDisposable>();

            var operationFailedChannelInformations = new List<FailedChannelInformation>();
            target.ChannelOperationFailedHandlers.Add((f) =>
            {
                operationFailedChannelInformations.Add(f);
                return TaskUtil.TrueCompletedTask;
            });
            var createdChannelInformations = new List<ChannelInformation>();            
            target.ChannelCreatedHandlers.Add((c) =>
            {            
                createdChannelInformations.Add(c);
                return TaskUtil.CompletedTask;
            });

            var discardedChannelInformations = new List<ChannelInformation>();
            target.ChannelDiscardedHandlers.Add((c) =>
            {
                discardedChannelInformations.Add(c);
                return TaskUtil.CompletedTask;
            });
            _clientChannel
                .Setup(c => c.SendMessageAsync(message, It.IsAny<CancellationToken>()))
                .Throws(exception);
            _clientChannel
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Throws(exception);
            _clientChannel
                .Setup(c => c.SendNotificationAsync(notification, It.IsAny<CancellationToken>()))
                .Throws(exception);
            _clientChannel
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .Throws(exception);
            _clientChannel
                .Setup(c => c.SendCommandAsync(command, It.IsAny<CancellationToken>()))
                .Throws(exception);
            _clientChannel
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .Throws(exception);
            _clientChannel
                .Setup(c => c.ProcessCommandAsync(It.IsAny<Command>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            _establishedClientChannelBuilder
                .SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_clientChannel.Object))
                .Returns(Task.FromResult(clientChannel2.Object));
            clientChannel2
                .SetupGet(c => c.SessionId)
                .Returns(sessionId);
            clientChannel2
                .SetupGet(c => c.Transport)
                .Returns(_transport.Object);
            clientChannel2
                .SetupGet(c => c.State)
                .Returns(SessionState.Established);
            clientChannel2
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(receivedMessage);
            clientChannel2
                .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(receivedNotification);
            clientChannel2
                .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(receivedCommand);
            clientChannel2
                .Setup(c => c.ProcessCommandAsync(It.IsAny<Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseCommand);

            // Act
            var sendMessageTask = Task.Run(() => target.SendMessageAsync(message, _cancellationToken));
            var sendNotificationTask = Task.Run(() => target.SendNotificationAsync(notification, _cancellationToken));
            var sendCommandTask = Task.Run(() => target.SendCommandAsync(command, _cancellationToken));
            var receiveMessageTask = Task.Run(() => target.ReceiveMessageAsync(_cancellationToken));
            var receiveNotificationTask = Task.Run(() => target.ReceiveNotificationAsync(_cancellationToken));
            var receiveCommandTask = Task.Run(() => target.ReceiveCommandAsync(_cancellationToken));
            var processTask = Task.Run(() => target.ProcessCommandAsync(requestCommand, _cancellationToken));
            await Task.WhenAll(sendMessageTask, sendNotificationTask, sendCommandTask, receiveMessageTask, receiveNotificationTask, receiveCommandTask, processTask);
            var actualReceivedMessage = await receiveMessageTask;
            var actualReceivedNotification = await receiveNotificationTask;
            var actualReceivedCommand = await receiveCommandTask;
            var actualProcessed = await processTask;

            // Assert
            _establishedClientChannelBuilder.Verify(c => c.BuildAndEstablishAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _clientChannelDisposable.Verify(c => c.Dispose(), Times.Once());
            clientChannel2.Verify(c => c.SendMessageAsync(message, _cancellationToken), Times.Once());
            clientChannel2.Verify(c => c.ReceiveMessageAsync(_cancellationToken), Times.Once());
            clientChannel2.Verify(c => c.SendNotificationAsync(notification, _cancellationToken), Times.Once());
            clientChannel2.Verify(c => c.ReceiveNotificationAsync(_cancellationToken), Times.Once());
            clientChannel2.Verify(c => c.SendCommandAsync(command, _cancellationToken), Times.Once());
            clientChannel2.Verify(c => c.ReceiveCommandAsync(_cancellationToken), Times.Once());
            clientChannel2.Verify(c => c.ProcessCommandAsync(requestCommand, _cancellationToken), Times.Once());
            clientChannel2Disposable.Verify(c => c.Dispose(), Times.Never());
            operationFailedChannelInformations.Count.ShouldBeGreaterThan(1);
            operationFailedChannelInformations.ShouldAllBe(e => e.Exception == exception);            
            createdChannelInformations.Count.ShouldBe(2);
            createdChannelInformations[0].SessionId.ShouldBe(_sessionId);
            createdChannelInformations[1].SessionId.ShouldBe(sessionId);
            discardedChannelInformations.Count.ShouldBe(1);
            discardedChannelInformations[0].SessionId.ShouldBe(_sessionId);            
            actualReceivedMessage.ShouldBe(receivedMessage);
            actualReceivedNotification.ShouldBe(receivedNotification);
            actualReceivedCommand.ShouldBe(receivedCommand);
            actualProcessed.ShouldBe(responseCommand);
        }
    }
}