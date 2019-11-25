using System;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Lime.Transport.WebSocket;
using NUnit.Framework;

namespace Lime.Protocol.LoadTests.WebSocket
{
    [TestFixture]
    public class PipeWebSocketTransportLoadTests : TransportLoadTestsBase
    {
        protected override Uri CreateUri() => new Uri("ws://localhost:8082");

        protected override ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer) 
            => new PipeWebSocketTransportListener(new[] { uri }, envelopeSerializer, null);

        protected override ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer) 
            => new PipeClientWebSocketTransport(envelopeSerializer);
    }
}
