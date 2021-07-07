using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Transport.AspNetCore.Transport;
using Moq;
using NUnit.Framework;
using Shouldly;
using static Lime.Protocol.UnitTests.Dummy;

namespace Lime.Transport.AspNetCore.UnitTests.Transport
{
    [TestFixture]
    public class SenderChannelAdapterTests : TestsBase
    {
        [SetUp]
        public void SetUp()
        {
            base.SetUp();
            Channel = new Mock<IChannel>();
            Transport = new Mock<ITransport>();
            Channel
                .SetupGet(c => c.Transport)
                .Returns(Transport.Object);
        }

        [TearDown]
        public new void TearDown()
        {
            base.TearDown();
        }
        
        private SenderChannelAdapter GetTarget() => new SenderChannelAdapter(Channel.Object);
        
        public Mock<IChannel> Channel { get; private set; }
        public Mock<ITransport> Transport { get; private set; }

        [Test]
        public async Task SendMessageAsync_AnyMessage_ShouldCallChannel()
        {
            // Arrange
            var message = CreateMessage(CreateTextContent());
            var target = GetTarget();
            
            // Act
            await target.SendMessageAsync(message, CancellationTokenSource.Token);
            
            // Assert
            Channel.Verify(c => c.SendMessageAsync(message, CancellationTokenSource.Token), Times.Once());
        }

        [Test]
        public async Task SendNotificationAsync_AnyNotification_ShouldCallChannel()
        {
            // Arrange
            var notification = CreateNotification(Event.Received);
            var target = GetTarget();
            
            // Act
            await target.SendNotificationAsync(notification, CancellationTokenSource.Token);
            
            // Assert
            Channel.Verify(c => c.SendNotificationAsync(notification, CancellationTokenSource.Token), Times.Once());
        }
        
                
        [Test]
        public async Task SendCommandAsync_AnyCommand_ShouldCallChannel()
        {
            // Arrange
            var command = CreateCommand(CreatePing());
            var target = GetTarget();
            
            // Act
            await target.SendCommandAsync(command, CancellationTokenSource.Token);
            
            // Assert
            Channel.Verify(c => c.SendCommandAsync(command, CancellationTokenSource.Token), Times.Once());
        }
        
        [Test]
        public async Task ProcessCommandAsync_AnyCommand_ShouldCallChannel()
        {
            // Arrange
            var requestCommand = CreateCommand(CreatePing());
            var responseCommand = CreateCommand(status: CommandStatus.Success);
            Channel
                .Setup(c => c.ProcessCommandAsync(requestCommand, It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseCommand)
                .Verifiable();
            var target = GetTarget();
            
            // Act
            var actual = await target.ProcessCommandAsync(requestCommand, CancellationTokenSource.Token);
            
            // Assert
            actual.ShouldBe(responseCommand);
            Channel.VerifyAll();
        }
    }
}