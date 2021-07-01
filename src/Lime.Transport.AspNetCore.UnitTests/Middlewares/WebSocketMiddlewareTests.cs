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
    public class WebSocketMiddlewareTests : TestsBase
    {
        [SetUp]
        public void SetUp()
        {
            base.SetUp(new TransportEndPoint()
            {
                Transport = TransportType.WebSocket,
                EndPoint = new IPEndPoint(IPAddress.Any, 443)
            });
            
            RequestDelegateExecutor = new RequestDelegateExecutor();
            WebSocket = new MockWebSocket(state: WebSocketState.Open);
            HttpContext = new Mock<HttpContext>();
            HttpResponse = new Mock<HttpResponse>();
            ConnectionInfo = new Mock<ConnectionInfo>();
            WebSocketManager = new Mock<WebSocketManager>();
            WebSocketManager
                .SetupGet(m => m.IsWebSocketRequest)
                .Returns(true);
            ConnectionInfo
                .SetupGet(c => c.LocalPort)
                .Returns(TransportEndPoint.EndPoint.Port);
            WebSocketManager
                .Setup(m => m.AcceptWebSocketAsync(It.IsAny<string>()))
                .ReturnsAsync(WebSocket);
            HttpContext
                .SetupGet(c => c.Connection)
                .Returns(ConnectionInfo.Object);
            HttpContext
                .SetupGet(c => c.WebSockets)
                .Returns(WebSocketManager.Object);
            HttpContext
                .SetupGet(c => c.RequestAborted)
                .Returns(CancellationTokenSource.Token);
            HttpContext
                .SetupGet(c => c.Response)
                .Returns(HttpResponse.Object);
        }

        public RequestDelegateExecutor RequestDelegateExecutor { get; set; }
        public Mock<ConnectionInfo> ConnectionInfo { get; set; }
        public MockWebSocket WebSocket { get; set; } 
        public Mock<WebSocketManager> WebSocketManager { get; set; }
        public Mock<HttpContext> HttpContext { get; set; }
        public Mock<HttpResponse> HttpResponse { get; set; }

        private WebSocketMiddleware GetTarget() => new WebSocketMiddleware(
            RequestDelegateExecutor.Next, 
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
            RequestDelegateExecutor.NextCalls.ShouldBe(0);
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
            RequestDelegateExecutor.NextCalls.ShouldBe(1);
            RequestDelegateExecutor.HttpContexts[0].ShouldBe(HttpContext.Object);
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