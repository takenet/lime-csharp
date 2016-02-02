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
        private Uri _serverUri;
        private TimeSpan _sendTimeout;
        private CancellationToken _cancellationToken;

        #region Scenario

        [SetUp]
        public void Setup()
        {
            _transport = new Mock<ITransport>();         
            _serverUri = Dummy.CreateUri("net.tcp", 55321);
            _sendTimeout = TimeSpan.FromSeconds(5);
            _cancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
        }

        [TearDown]
        public void Teardown()
        {
            _transport = null;
        }

        #endregion

        private ClientChannelBuilder GetTarget()
        {
            return ClientChannelBuilder.Create(_transport.Object, serverUri);
        }

        [Test]
        public async Task Create_ValidTransportAndUri_CallsTransportOpen()
        {
            // Arrange            
            _transport.Setup(t => t.IsConnected).Returns(false);
            _transport
                .Setup(t => t.OpenAsync(serverUri, cancellationToken))
                .Returns(TaskUtil.CompletedTask)
                .Verifiable();
            var target = GetTarget();

            // Act            
            var channel = await target.BuildAsync(_cancellationToken);

            // Assert
            _transport.Verify();
            channel.Transport.ShouldBe(_transport.Object);
        }

        [Test]
        public async Task Create_TransportIsConnected_DoNotCallTransportOpen()
        {
            // Arrange            
            _transport.Setup(t => t.IsConnected).Returns(true);
            var target = GetTarget();
            
            // Act
            var channel = await target.BuildAsync(_cancellationToken);

            // Assert            
            channel.Transport.ShouldBe(_transport.Object);
            _transport.Verify(t => t.OpenAsync(serverUri, _cancellationToken), Times.Never);
        }
        
        [Test]
        public async Task AddMessageModule_NewMessageModule_AddsToCreatedChannel()
        {
            // Arrange            
            _transport.Setup(t => t.IsConnected).Returns(true);                        
            var moduleMock = new Mock<IChannelModule<Message>>();
            var target = GetTarget();
                        
            // Act
            target.AddMessageModule(moduleMock.Object);            
            var channel = await target.BuildAsync(_cancellationToken);

            // Assert            
            channel.MessageModules.ShouldContains(target.Object);
        }
        
        [Test]
        public async Task AddCommandModule_NewCommandModule_AddsToCreatedChannel()
        {
            // Arrange            
            _transport.Setup(t => t.IsConnected).Returns(true);                        
            var moduleMock = new Mock<IChannelModule<Command>>();
            var target = GetTarget();
                        
            // Act
            target.AddCommandModule(moduleMock.Object);            
            var channel = await target.BuildAsync(_cancellationToken);

            // Assert            
            channel.CommandModules.ShouldContains(target.Object);
        }
        
        [Test]
        public async Task AddNotificationModule_NewNotificationModule_AddsToCreatedChannel()
        {
            // Arrange            
            _transport.Setup(t => t.IsConnected).Returns(true);                        
            var moduleMock = new Mock<IChannelModule<Notification>>();
            var target = GetTarget();
                        
            // Act
            target.AddNotificationModule(moduleMock.Object);            
            var channel = await target.BuildAsync(_cancellationToken);

            // Assert            
            channel.NotificationModules.ShouldContains(target.Object);
        }       
        
        [Test]
        public async Task AddBuiltHandler_CallsBuildAsync_ShouldExecuteHandler()
        {
            // Arrange            
            _transport.Setup(t => t.IsConnected).Returns(true);                        
            var moduleMock = new Mock<IChannelModule<Notification>>();                        
            IClientChannel handlerClientChannel = null;
            CancellationToken handlerCancellationToken = default(CancellationToken);
            Func<IClientChannel, CancellationToken, Task> builtHandler = async (clientChannel, cancellationToken) =>
            {
                handlerClientChannel = clientChannel;
                handlerCancellationToken = cancellationToken;                
            };
            
            var target = GetTarget();
            
            // Act
            target.AddBuiltHandler(builtHandler);            
            var channel = await target.BuildAsync(_cancellationToken);

            // Assert            
            handlerClientChannel.ShouldNotBeNull();
            handlerClientChannel.ShouldBe(channel);
            cancellationToken.ShouldBe(_cancellationToken);
        }        
    }
}
