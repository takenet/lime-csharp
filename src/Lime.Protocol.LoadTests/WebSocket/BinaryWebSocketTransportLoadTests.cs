using System;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Lime.Transport.WebSocket;

namespace Lime.Protocol.LoadTests.WebSocket
{
    public class BinaryWebSocketTransportLoadTests : TransportLoadTestsBase
    {
        protected override Uri CreateUri() => new Uri("ws://localhost:8081");
        
        protected override ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer)
            => new WebSocketTransportListener(uri, null, envelopeSerializer, webSocketMessageType: System.Net.WebSockets.WebSocketMessageType.Binary);

        protected override ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer) =>
            new ClientWebSocketTransport(envelopeSerializer,
                webSocketMessageType: System.Net.WebSockets.WebSocketMessageType.Binary);
    }
}
