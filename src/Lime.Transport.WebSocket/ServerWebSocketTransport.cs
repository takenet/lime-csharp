using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;

namespace Lime.Transport.WebSocket
{
    public class ServerWebSocketTransport : WebSocketTransport
    {
        private readonly HttpListenerWebSocketContext _context;

        internal ServerWebSocketTransport(
            HttpListenerWebSocketContext context, 
            IEnvelopeSerializer envelopeSerializer, 
            ITraceWriter traceWriter = null, 
            int bufferSize = 8192,
            WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text)
            : base(context.WebSocket, envelopeSerializer, traceWriter, bufferSize, webSocketMessageType)
        {
            _context = context;
        }

        public override IReadOnlyDictionary<string, object> Options
        {
            get
            {
                var options = (Dictionary<string, object>)base.Options;
                options.Add(nameof(HttpListenerWebSocketContext.IsAuthenticated), _context.IsAuthenticated);
                options.Add(nameof(HttpListenerWebSocketContext.IsLocal), _context.IsLocal);
                options.Add(nameof(HttpListenerWebSocketContext.IsSecureConnection), _context.IsSecureConnection);
                options.Add(nameof(HttpListenerWebSocketContext.Origin), _context.Origin);
                options.Add(nameof(HttpListenerWebSocketContext.SecWebSocketKey), _context.SecWebSocketKey);
                options.Add(nameof(HttpListenerWebSocketContext.SecWebSocketVersion), _context.SecWebSocketVersion);
                return options;                
            }
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken) => Task.CompletedTask;

        protected override async Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            if (WebSocket.State == WebSocketState.Open)
            {
                // Give a little delay to give time to the "finished" session reach the client.
                // Sometimes the websocket doesn't flush the sent data.
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            await base.PerformCloseAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
