using System;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Lime.Transport.WebSocket;
using Lime.Transport.WebSocket.Kestrel;
using NUnit.Framework;

namespace Lime.Protocol.LoadTests.WebSocket
{
    [TestFixture]
    public class KestrelWebSocketLoadTests : TransportLoadTestsBase
    {
        protected override Uri CreateUri() => new Uri("ws://localhost:8081");

        protected override ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer) 
            => new KestrelWebSocketTransportListener(new[] { uri }, envelopeSerializer, null);

        protected override ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer) 
            => new ClientWebSocketTransport(envelopeSerializer);
    }
}
