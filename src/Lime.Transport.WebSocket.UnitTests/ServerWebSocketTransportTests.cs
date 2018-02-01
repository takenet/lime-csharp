using System;
using Lime.Protocol.Network.UnitTests;

namespace Lime.Transport.WebSocket.UnitTests
{
    public class ServerWebSocketTransportTests : ServerTransportTestsBase<ServerWebSocketTransport, ClientWebSocketTransport, WebSocketTransportListener>
    {
        public ServerWebSocketTransportTests()
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