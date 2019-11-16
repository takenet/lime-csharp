using System;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests.Common.Network;
using NUnit.Framework;

namespace Lime.Transport.WebSocket.UnitTests
{
    [TestFixture]
    public class WebSocketTransportTests : TransportTestsBase
    {
        protected override Uri CreateListenerUri() => new Uri("ws://localhost:8081");

        protected override ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer) 
            => new WebSocketTransportListener(ListenerUri, null, envelopeSerializer, TraceWriter, closeGracefully: false);

        protected override ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer) => new ClientWebSocketTransport(envelopeSerializer, closeGracefully: false);
    }
}