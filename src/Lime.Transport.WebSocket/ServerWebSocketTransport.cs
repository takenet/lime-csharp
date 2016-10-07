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
            int bufferSize = 8192)
            : base(context.WebSocket, envelopeSerializer, traceWriter, bufferSize)
        {
            _context = context;
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken) 
            => Task.CompletedTask;



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
    }
}
