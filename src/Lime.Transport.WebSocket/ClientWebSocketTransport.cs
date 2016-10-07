using System;
using System.Net.WebSockets;
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

        protected override async Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            var clientWebSocket = ((ClientWebSocket) WebSocket);
            clientWebSocket.Options.AddSubProtocol(LimeUri.LIME_URI_SCHEME);
            await clientWebSocket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
            await base.PerformOpenAsync(uri, cancellationToken);
        }

        protected override async Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                await WebSocket.CloseAsync(CloseStatus, CloseStatusDescription, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                StopListenerTask();
            }
        }
    }
}
