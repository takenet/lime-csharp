using System;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests.Network;
using NUnit.Framework;

namespace Lime.Transport.WebSocket.UnitTests
{
    [TestFixture]
    public class PipeWebSocketTransportListenerTests : TransportListenerTestsBase
    {
        protected override ITransport CreateClientTransport() => new PipeClientWebSocketTransport(EnvelopeSerializer);

        protected override Uri CreateListenerUri() => new Uri("ws://localhost:8092");

        protected override ITransportListener CreateTransportListener() 
            => new PipeWebSocketTransportListener(new[] { ListenerUri }, EnvelopeSerializer, null, TraceWriter.Object);
    }
}