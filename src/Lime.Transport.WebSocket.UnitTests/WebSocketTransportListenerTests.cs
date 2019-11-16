using System;
using Lime.Protocol.UnitTests.Network;
using NUnit.Framework;

namespace Lime.Transport.WebSocket.UnitTests
{
    [TestFixture]
    public class WebSocketTransportListenerTests : TransportListenerTestsBase<ClientWebSocketTransport, WebSocketTransportListener>
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
            return new WebSocketTransportListener(new[] { ListenerUri }, EnvelopeSerializer, null, TraceWriter.Object);
        }
    }
}