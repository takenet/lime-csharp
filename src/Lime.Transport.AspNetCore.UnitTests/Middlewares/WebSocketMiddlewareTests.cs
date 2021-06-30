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
        }

        public RequestDelegateExecutor RequestDelegateExecutor { get; set; }
        public Mock<ConnectionInfo> ConnectionInfo { get; set; }
        public MockWebSocket WebSocket { get; set; } 
        public Mock<WebSocketManager> WebSocketManager { get; set; }
        public Mock<HttpContext> HttpContext { get; set; }

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
    }
}