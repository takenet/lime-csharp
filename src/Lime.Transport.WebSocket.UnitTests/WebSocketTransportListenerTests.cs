using System;
using Lime.Protocol.UnitTests.Network;

namespace Lime.Transport.WebSocket.UnitTests
{
    public class WebSocketTransportListenerTests : TransportListenerTestsBase<ClientWebSocketTransport, WebSocketTransportListener>
    {
        public WebSocketTransportListenerTests()
            : base(new Uri("ws://localhost:8081"))
        {

        }

        protected override ClientWebSocketTransport CreateClientTransport()
        {
            return new ClientWebSocketTransport(EnvelopeSerializer);
        }

        protected override WebSocketTransportListener CreateTransportListener()
        {
            return new WebSocketTransportListener(ListenerUri, null, EnvelopeSerializer, TraceWriter.Object);
        }
    }
}