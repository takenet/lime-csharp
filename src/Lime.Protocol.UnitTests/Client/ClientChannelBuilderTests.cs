using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Protocol.Util;
using Moq;
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
        

        private ClientChannelBuilder GetTarget()
        {
            return ClientChannelBuilder.Create(_transport.Object, _serverUri);
        }

        [Test]
        public async Task Create_ValidTransportAndUri_CallsTransportOpen()
        {
            // Arrange            
            _transport.Setup(t => t.IsConnected).Returns(false);
            _transport
                .Setup(t => t.OpenAsync(_serverUri, _cancellationToken))
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
            _transport.Verify(t => t.OpenAsync(_serverUri, _cancellationToken), Times.Never);
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
            channel.MessageModules.ShouldContain(moduleMock.Object);
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
            channel.CommandModules.ShouldContain(moduleMock.Object);
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
            channel.NotificationModules.ShouldContain(moduleMock.Object);
        }       
        
        [Test]
        public async Task AddBuiltHandler_SingleHandler_ShouldExecuteHandler()
        {
            // Arrange            
            _transport.Setup(t => t.IsConnected).Returns(true);                        
            IClientChannel handlerClientChannel = null;
            var handlerCancellationToken = default(CancellationToken);
            Func<IClientChannel, CancellationToken, Task> builtHandler = (clientChannel, cancellationToken) =>
            {
                handlerClientChannel = clientChannel;
                handlerCancellationToken = cancellationToken;
                return TaskUtil.CompletedTask;
            };
            
            var target = GetTarget();
            
            // Act
            target.AddBuiltHandler(builtHandler);            
            var channel = await target.BuildAsync(_cancellationToken);

            // Assert            
            handlerClientChannel.ShouldNotBeNull();
            handlerClientChannel.ShouldBe(channel);
            handlerCancellationToken.ShouldBe(_cancellationToken);
        }

        [Test]
        public async Task AddBuiltHandler_HandlerThrowsException_ShouldRethrowToCaller()
        {
            // Arrange            
            var exception = Dummy.CreateException<ApplicationException>();
            _transport.Setup(t => t.IsConnected).Returns(true);            
            Func<IClientChannel, CancellationToken, Task> builtHandler = (clientChannel, cancellationToken) =>
            {
                throw exception;
            };
            var target = GetTarget();

            // Act
            target.AddBuiltHandler(builtHandler);

            await target.BuildAsync(_cancellationToken).ShouldThrowAsync<ApplicationException>();
        }

        [Test]
        public async Task AddBuiltHandler_MultipleHandlers_ShouldExecuteHandlers()
        {
            // Arrange            
            _transport.Setup(t => t.IsConnected).Returns(true);
            var handlerClientChannels = new List<IClientChannel>();
            var handlerCancellationTokens = new List<CancellationToken>();            
            var target = GetTarget();

            // Act
            var count = Dummy.CreateRandomInt(100);
            for (int i = 0; i < count; i++)
            {
                Func<IClientChannel, CancellationToken, Task> builtHandler = (clientChannel, cancellationToken) =>
                {
                    handlerClientChannels.Add(clientChannel);
                    handlerCancellationTokens.Add(cancellationToken);
                    return TaskUtil.CompletedTask;
                };
                target.AddBuiltHandler(builtHandler);
            }
            var channel = await target.BuildAsync(_cancellationToken);

            // Assert            
            handlerClientChannels.Count.ShouldBe(count);
            handlerCancellationTokens.Count.ShouldBe(count);
            handlerClientChannels.ShouldAllBe(c => c == channel);
            handlerCancellationTokens.ShouldAllBe(t => t == _cancellationToken);            
        }
    }
}
