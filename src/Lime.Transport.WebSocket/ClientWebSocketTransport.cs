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
        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);


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

        protected override async Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            if (WebSocket.State == WebSocketState.Open || 
                WebSocket.State == WebSocketState.CloseReceived)
            {
                using (var cts = new CancellationTokenSource(CloseTimeout))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token))
                {
                    try
                    {
                        // Initiate the close handshake
                        await
                            WebSocket.CloseAsync(CloseStatus, CloseStatusDescription, linkedCts.Token)
                                .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cts.IsCancellationRequested)
                    {
                        await CloseWebSocketOutputAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
