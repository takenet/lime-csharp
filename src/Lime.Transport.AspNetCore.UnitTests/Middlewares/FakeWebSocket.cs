using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    public class FakeWebSocket : System.Net.WebSockets.WebSocket
    {
        private WebSocketState _state;

        public FakeWebSocket(
            WebSocketCloseStatus? closeStatus = null,
            string closeStatusDescription = "",
            WebSocketState state = default,
            string subProtocol = ""
        )
        {
            CloseStatus = closeStatus;
            CloseStatusDescription = closeStatusDescription;
            _state = state;
            SubProtocol = subProtocol;
        }

        public override WebSocketCloseStatus? CloseStatus { get; }
        public override string CloseStatusDescription { get; }

        public override WebSocketState State => _state;
        public override string SubProtocol { get; }
        public bool AbortCalled { get; set; }
        public bool CloseCalled { get; set; }
        public bool CloseOutputCalled { get; set; }
        public bool DisposedCalled { get; set; }
        public int ReceiveCalls { get; set; }
        public int SendCalls { get; set; }

        public Action ReceiveHandler;
        
        public override async Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer, 
            CancellationToken cancellationToken)
        {
            ReceiveCalls++;
            ReceiveHandler?.Invoke();
            await cancellationToken.AsTask();

            return new WebSocketReceiveResult(0, WebSocketMessageType.Text, true);
        }
        
        public override Task SendAsync(
            ArraySegment<byte> buffer, 
            WebSocketMessageType messageType, 
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            SendCalls++;
            return Task.CompletedTask;
        }

        public override void Abort()
        {
            AbortCalled = true;
        }
        
        public override Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            CloseCalled = true;
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }
        
        public override Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            CloseOutputCalled = true;
            return Task.CompletedTask;
        }
        
        public override void Dispose()
        {
            _state = WebSocketState.Closed;
            DisposedCalled = true;
        }
    }
}