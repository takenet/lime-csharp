using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Client
{
    [TestFixture]
    public class MultiplexerClientChannelTests
    {
        private TimeSpan _sendTimeout;
        private CancellationToken _cancellationToken;
        private int _count = 5;
        private Mock<IEstablishedClientChannelBuilder> _establishedClientChannelBuilder;
        private Mock<IClientChannelBuilder> _clientChannelBuilder;
        private Mock<IClientChannel>[] _clientChannels;
        private Mock<ITransport>[] _transports;
        private string[] _sessionIds;
        private BufferBlock<Message>[] _receiveMessageBuffers;
        private BufferBlock<Notification>[] _receiveNotificationBuffers;
        private BufferBlock<Command>[] _receiveCommandBuffers;
        private ConcurrentQueue<Message>[] _sentMessageBuffers;
        private ConcurrentQueue<Notification>[] _sentNotificationBuffers;
        private ConcurrentQueue<Command>[] _sentCommandBuffers;

        [SetUp]
        public void Setup()
        {
            _sendTimeout = TimeSpan.FromSeconds(15);
            _cancellationToken = _sendTimeout.ToCancellationToken();
            _receiveMessageBuffers = new BufferBlock<Message>[_count];
            _receiveNotificationBuffers = new BufferBlock<Notification>[_count];
            _receiveCommandBuffers = new BufferBlock<Command>[_count];
            _sentMessageBuffers = new ConcurrentQueue<Message>[_count];
            _sentNotificationBuffers = new ConcurrentQueue<Notification>[_count];
            _sentCommandBuffers = new ConcurrentQueue<Command>[_count];
            _clientChannels = new Mock<IClientChannel>[_count];
            _sessionIds = new string[_count];
            _transports = new Mock<ITransport>[_count];
            for (int i = 0; i < _count; i++)
            {
                _sessionIds[i] = EnvelopeId.NewId();
                _transports[i] = new Mock<ITransport>();
                _transports[i]
                    .SetupGet(t => t.IsConnected)
                    .Returns(true);
                _clientChannels[i] = new Mock<IClientChannel>();
                _clientChannels[i]
                    .SetupGet(c => c.SessionId)
                    .Returns(_sessionIds[i]);
                _clientChannels[i]
                    .SetupGet(c => c.Transport)
                    .Returns(_transports[i].Object);
                _clientChannels[i]
                    .SetupGet(c => c.State)
                    .Returns(SessionState.Established);
                _receiveMessageBuffers[i] = new BufferBlock<Message>();
                _receiveNotificationBuffers[i] = new BufferBlock<Notification>();
                _receiveCommandBuffers[i] = new BufferBlock<Command>();
                _sentMessageBuffers[i] = new ConcurrentQueue<Message>();
                _sentNotificationBuffers[i] = new ConcurrentQueue<Notification>();
                _sentCommandBuffers[i] = new ConcurrentQueue<Command>();
                var index = i;
                _clientChannels[i]
                    .Setup(c => c.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns((CancellationToken cancellationToken) => _receiveMessageBuffers[index].ReceiveAsync(cancellationToken));
                _clientChannels[i]
                    .Setup(c => c.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                    .Returns((CancellationToken cancellationToken) => _receiveNotificationBuffers[index].ReceiveAsync(cancellationToken));
                _clientChannels[i]
                    .Setup(c => c.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                    .Returns((CancellationToken cancellationToken) => _receiveCommandBuffers[index].ReceiveAsync(cancellationToken));
                _clientChannels[i]
                    .Setup(c => c.SendMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask)
                    .Callback(
                        (Message message, CancellationToken cancellationToken) =>
                            _sentMessageBuffers[index].Enqueue(message));
                _clientChannels[i]
                    .Setup(c => c.SendNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask)
                    .Callback(
                        (Notification notification, CancellationToken cancellationToken) =>
                            _sentNotificationBuffers[index].Enqueue(notification));
                _clientChannels[i]
                    .Setup(c => c.SendCommandAsync(It.IsAny<Command>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask)
                    .Callback(
                        (Command command, CancellationToken cancellationToken) =>
                            _sentCommandBuffers[index].Enqueue(command));
            }

            _clientChannelBuilder = new Mock<IClientChannelBuilder>();
            _clientChannelBuilder
                .SetupGet(b => b.SendTimeout)
                .Returns(_sendTimeout);
            _establishedClientChannelBuilder = new Mock<IEstablishedClientChannelBuilder>();
            _establishedClientChannelBuilder
                .SetupGet(b => b.ChannelBuilder)
                .Returns(_clientChannelBuilder.Object);
            _establishedClientChannelBuilder
                .Setup(b => b.Copy())
                .Returns(() => _establishedClientChannelBuilder.Object);
            _establishedClientChannelBuilder
                .Setup(b => b.WithInstance(It.IsAny<string>()))
                .Returns(() => _establishedClientChannelBuilder.Object);
            var buildSequence = _establishedClientChannelBuilder.SetupSequence(b => b.BuildAndEstablishAsync(It.IsAny<CancellationToken>()));
            for (int i = 0; i < _count; i++)
            {
                buildSequence = buildSequence.Returns(_clientChannels[i].Object.AsCompletedTask());
            }        
        }

        private MultiplexerClientChannel GetTarget()
        {
            return new MultiplexerClientChannel(_establishedClientChannelBuilder.Object, _count);            
        }

        private async Task<MultiplexerClientChannel> GetTargetAndEstablishAsync()
        {
            var channel = GetTarget();
            await channel.EstablishAsync(_cancellationToken);
            return channel;
        }

        [Test]
        public async Task ReceiveCommandAsync_ResponseWithourPendingRequest_ShouldSucceed()
        {
            // Arrange
            var responseCommand = Dummy.CreateCommand(Dummy.CreatePing(), CommandMethod.Get, CommandStatus.Success);
            var target = await GetTargetAndEstablishAsync();            

            // Act
            var receiveCommandTask = target.ReceiveCommandAsync(_cancellationToken);
            var responseIndex = ((_count * 3) / 2) % _count;
            await _receiveCommandBuffers[responseIndex].SendAsync(responseCommand, _cancellationToken);
            var actual = await receiveCommandTask;

            // Assert
            actual.ShouldBe(responseCommand);
        }

        [Test]
        public async Task ProcessCommandAsync_ResponseReceivedFromSameChannel_ShouldSucceed()
        {
            // Arrange
            var requestCommand = Dummy.CreateCommand();
            var responseCommand = Dummy.CreateCommand(Dummy.CreatePing(), CommandMethod.Get, CommandStatus.Success);
            responseCommand.Id = requestCommand.Id;
            var target = await GetTargetAndEstablishAsync();

            // Act
            var processCommandTask = target.ProcessCommandAsync(requestCommand, _cancellationToken);
            await Task.Delay(500, _cancellationToken);
            // Determine in which channel the command was sent
            var requestIndex = Enumerable.Range(0, _count).First(i => _sentCommandBuffers[i].Contains(requestCommand));
            var responseIndex = requestIndex; 
            await _receiveCommandBuffers[responseIndex].SendAsync(responseCommand, _cancellationToken);
            var actual = await processCommandTask;

            // Assert
            actual.ShouldBe(responseCommand);
        }

        [Test]
        public async Task ProcessCommandAsync_ResponseReceivedFromDifferentChannel_ShouldSucceed()
        {
            // Arrange
            var requestCommand = Dummy.CreateCommand();
            var responseCommand = Dummy.CreateCommand(Dummy.CreatePing(), CommandMethod.Get, CommandStatus.Success);
            responseCommand.Id = requestCommand.Id;
            var target = await GetTargetAndEstablishAsync();

            // Act
            var processCommandTask = target.ProcessCommandAsync(requestCommand, _cancellationToken);
            await Task.Delay(500, _cancellationToken);
            // Determine in which channel the command was sent
            var requestIndex = Enumerable.Range(0, _count).First(i => _sentCommandBuffers[i].Contains(requestCommand));
            var responseIndex = (requestIndex + 1)%_count;
            await _receiveCommandBuffers[responseIndex].SendAsync(responseCommand, _cancellationToken);
            var actual = await processCommandTask;

            // Assert
            actual.ShouldBe(responseCommand);
        }
    }
    
}
