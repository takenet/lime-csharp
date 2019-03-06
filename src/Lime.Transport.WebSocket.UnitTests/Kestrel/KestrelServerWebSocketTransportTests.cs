using System;
using Lime.Protocol.UnitTests.Common.Network;
using Lime.Transport.WebSocket.Kestrel;
using NUnit.Framework;

namespace Lime.Transport.WebSocket.UnitTests.Kestrel
{
    [TestFixture]
    public class KestrelServerWebSocketTransportTests : ServerTransportTestsBase<KestrelServerWebSocketTransport, ClientWebSocketTransport, KestrelWebSocketTransportListener>
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

        protected override KestrelWebSocketTransportListener CreateTransportListener()
        {
            return new KestrelWebSocketTransportListener(new[] { ListenerUri }, EnvelopeSerializer, null, TraceWriter.Object);
        }
    }
}