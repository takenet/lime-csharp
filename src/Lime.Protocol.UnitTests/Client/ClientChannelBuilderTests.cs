using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Protocol.Util;
using Moq;
using NUnit.Core;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Client
{
    [TestFixture]
    public class ClientChannelBuilderTests
    {
        private Mock<ITransport> _transport;


        #region Scenario

        [SetUp]
        public void Setup()
        {
            _transport = new Mock<ITransport>();            
        }


        [TearDown]
        public void Teardown()
        {
            _transport = null;
        }

        #endregion


        [Test]
        public async Task Create_ValidTransportAndUri_CallsTransportOpen()
        {
            // Arrange
            var serverUri = Dummy.CreateUri("net.tcp", 55321);
            var cancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
            _transport.Setup(t => t.IsConnected).Returns(false);
            _transport
                .Setup(t => t.OpenAsync(serverUri, cancellationToken))
                .Returns(TaskUtil.CompletedTask)
                .Verifiable();

            // Act
            var builder = ClientChannelBuilder.Create(_transport.Object, serverUri);
            var channel = await builder.BuildAsync(cancellationToken);

            // Assert
            _transport.Verify();
            channel.Transport.ShouldBe(_transport.Object);
        }

        [Test]
        public async Task Create_TransportIsConnected_DoNotCallTransportOpen()
        {
            // Arrange
            var serverUri = Dummy.CreateUri("net.tcp", 55321);
            var cancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
            _transport.Setup(t => t.IsConnected).Returns(true);
            
            // Act
            var builder = ClientChannelBuilder.Create(_transport.Object, serverUri);
            var channel = await builder.BuildAsync(cancellationToken);

            // Assert            
            channel.Transport.ShouldBe(_transport.Object);
            _transport.Verify(t => t.OpenAsync(serverUri, cancellationToken), Times.Never);
        }
    }
}
