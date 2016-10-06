using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;

namespace Lime.Transport.WebSocket
{
    public class ClientWebSocketTransport : WebSocketTransport, ITransport
    {                        
        public ClientWebSocketTransport(
            IEnvelopeSerializer envelopeSerializer, 
            ITraceWriter traceWriter = null, 
            int bufferSize = 8192)
            : base(new ClientWebSocket(), envelopeSerializer, traceWriter, bufferSize)
        {                        
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            var clientWebSocket = ((ClientWebSocket) WebSocket);
            clientWebSocket.Options.AddSubProtocol(LimeUri.LIME_URI_SCHEME);
            return clientWebSocket.ConnectAsync(uri, cancellationToken);
        }
    }
}
