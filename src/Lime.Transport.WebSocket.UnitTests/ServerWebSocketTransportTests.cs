using System;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests.Common.Network;
using NUnit.Framework;

namespace Lime.Transport.WebSocket.UnitTests
{
    [TestFixture]
    public class ServerWebSocketTransportTests : ServerTransportTestsBase
    {
        protected override Uri CreateListenerUri() => new Uri("ws://localhost:8081");

        protected override ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer) 
            => new WebSocketTransportListener(ListenerUri, null, EnvelopeSerializer, TraceWriter.Object);

        protected override ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer) => new ClientWebSocketTransport(EnvelopeSerializer);
    }
}