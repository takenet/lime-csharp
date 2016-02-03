using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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


        private SessionCompression[] _compressionOptions;
        private SessionCompression _compression;

        private SessionEncryption[] _encryptionOptions;
        private SessionEncryption _encryption;

        private SessionAuthentication[] _authenticationSchemes;
        private SessionAuthentication _authenticationScheme;

        private Session _negotiatingSession;
        private Session _authenticatingSession;
        private Session _establishedSession;

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

            _negotiatingSession = Dummy.CreateSession(SessionState.Negotiating);
            _authenticatingSession = Dummy.CreateSession(SessionState.Authenticating);
            _establishedSession = Dummy.CreateSession(SessionState.Established);

            _clientChannel
                .Setup(c => c.StartNewSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_negotiatingSession);

            

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
