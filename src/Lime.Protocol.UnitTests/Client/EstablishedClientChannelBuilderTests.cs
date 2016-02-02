using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    public class EstablishedClientChannelBuilderTests
    {
        private CancellationToken _cancellationToken;
        private Mock<IClientChannel> _clientChannel;
        private Mock<IClientChannelBuilder> _clientChannelBuilder;
                
        #region Scenario

        [SetUp]
        public void Setup()
        {                  
            _cancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
            _clientChannel = new Mock<IClientChannel>();
            _clientChannelBuilder = new Mock<IClientChannelBuilder>();
            _clientChannelBuilder
                .Setup(b => b.BuildAsync(_cancellationToken))
                .ReturnsAsync(_clientChannel.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _clientChannelBuilder = null;
            _clientChannel = null;
        }

        #endregion

        private EstablishedClientChannelBuilder GetTarget()
        {
            return new EstablishedClientChannelBuilder(_clientChannelBuilder.Object);
        }

        //[Test]
        //public async Task WithCompression_()
        //{
        //    // Arrange            
        //    _transport.Setup(t => t.IsConnected).Returns(true);
        //    var target = GetTarget();

        //    // Act            
        //    var channel = await target.BuildAndEstablishAsync(_cancellationToken);

        //    // Assert
        //    //_transport.Verify();
        //    //channel.Transport.ShouldBe(_transport.Object);
        //}       
    }
}
