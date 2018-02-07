using System;
using Lime.Protocol.Network.UnitTests;
using NUnit.Framework;

namespace Lime.Transport.WebSocket.UnitTests
{
    [TestFixture]
    public class ServerWebSocketTransportTests : ServerTransportTestsBase<ServerWebSocketTransport, ClientWebSocketTransport, WebSocketTransportListener>
    {
        [SetUp]
        public void SetUp()
        {
            SetUp(new Uri("ws://localhost:8081"));
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