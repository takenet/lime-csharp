using System;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests.Network;
using NUnit.Framework;

namespace Lime.Transport.WebSocket.UnitTests
{
    [TestFixture]
    public class WebSocketTransportListenerTests : TransportListenerTestsBase
    {
        protected override ITransport CreateClientTransport() => new ClientWebSocketTransport(EnvelopeSerializer);

        protected override Uri CreateListenerUri() => new Uri("ws://localhost:8081");

        protected override ITransportListener CreateTransportListener() 
            => new WebSocketTransportListener(new[] { ListenerUri }, EnvelopeSerializer, null, TraceWriter.Object);
    }
}