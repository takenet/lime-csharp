using System;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests.Common.Network;
using Lime.Transport.WebSocket.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using NUnit.Framework;
using ITransport = Lime.Protocol.Network.ITransport;

namespace Lime.Transport.WebSocket.UnitTests.Kestrel
{
    [TestFixture]
    public class KestrelWebSocketTransportTests : TransportTestsBase
    {
        protected override Uri CreateListenerUri() => new Uri("ws://localhost:8081");

        protected override ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer)
        {
            return new KestrelWebSocketTransportListener(new[] { uri }, envelopeSerializer, null, TraceWriter.Object);
        }

        protected override ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer)
        {
            return new ClientWebSocketTransport(envelopeSerializer);
        }
    }
}