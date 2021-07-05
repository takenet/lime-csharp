using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Lime.Transport.AspNetCore.Middlewares;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    [TestFixture]
    public class WebSocketMiddlewareTests : HttpMiddlewareTestsBase
    {
        [SetUp]
        public void SetUp()
        {
            base.SetUp(new TransportEndPoint()
            {
                Transport = TransportType.WebSocket,
                EndPoint = new IPEndPoint(IPAddress.Any, 443)
            });
            
            WebSocket = new FakeWebSocket(state: WebSocketState.Open);
            WebSocketManager = new Mock<WebSocketManager>();
            WebSocketManager
                .SetupGet(m => m.IsWebSocketRequest)
                .Returns(true);
            WebSocketManager
                .Setup(m => m.AcceptWebSocketAsync(It.IsAny<string>()))
                .ReturnsAsync(WebSocket);
            HttpContext
                .SetupGet(c => c.WebSockets)
                .Returns(WebSocketManager.Object);
        }
        
        [TearDown]
        public new void TearDown()
        {
            base.TearDown();
        }
        
        public FakeWebSocket WebSocket { get; set; } 
        public Mock<WebSocketManager> WebSocketManager { get; set; }

        private WebSocketMiddleware GetTarget() => new WebSocketMiddleware(
            FakeRequestDelegate.Next, 
            EnvelopeSerializer, 
            TransportListener, 
            Microsoft.Extensions.Options.Options.Create(Options));

        [Test]
        public async Task Invoke_WebSocketRequest_ShouldReadFromTransport()
        {
            // Arrange
            var target = GetTarget();
            WebSocket.ReceiveHandler = () => CancellationTokenSource.Cancel();

            // Act
            try
            {
                await target.Invoke(HttpContext.Object);
            }
            catch (OperationCanceledException ex) 
                when (ex.CancellationToken == CancellationTokenSource.Token)
            {
                
            }

            // Assert
            WebSocket.ReceiveCalls.ShouldBeGreaterThanOrEqualTo(1);
        }

        [Test]
        public async Task Invoke_WebSocketRequest_ShouldNotCallNext()
        {
            // Arrange
            var target = GetTarget();
            WebSocket.ReceiveHandler = () => CancellationTokenSource.Cancel();

            // Act
            try
            {
                await target.Invoke(HttpContext.Object);
            }
            catch (OperationCanceledException ex) 
                when (ex.CancellationToken == CancellationTokenSource.Token)
            {
                
            }

            // Assert
            FakeRequestDelegate.NextCalls.ShouldBe(0);
        }
        
        [Test]
        public async Task Invoke_NonWebSocketPort_ShouldCallNextMiddleware()
        {
            // Arrange
            ConnectionInfo
                .SetupGet(c => c.LocalPort)
                .Returns(80);
            
            var target = GetTarget();

            // Act
            await target.Invoke(HttpContext.Object);

            // Assert
            FakeRequestDelegate.NextCalls.ShouldBe(1);
            FakeRequestDelegate.HttpContexts[0].ShouldBe(HttpContext.Object);
        }
        
        [Test]
        public async Task Invoke_NonWebSocketRequest_ShouldReturnBadRequest()
        {
            // Arrange
            WebSocketManager
                .SetupGet(m => m.IsWebSocketRequest)
                .Returns(false);
            var target = GetTarget();

            // Act
            await target.Invoke(HttpContext.Object);

            // Assert
            HttpResponse.VerifySet(r => r.StatusCode = 400);
        }
    }
}