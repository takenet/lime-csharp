using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Microsoft.AspNetCore.Http;

namespace Lime.Transport.WebSocket.Kestrel
{
    public class KestrelServerWebSocketTransport : WebSocketTransport
    {
        private readonly HttpContext _context;

        private readonly TaskCompletionSource<object> _executionTaskCompletionSource;

        internal KestrelServerWebSocketTransport(
            HttpContext context,
            System.Net.WebSockets.WebSocket webSocket,
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null,
            int bufferSize = 8192,
            WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text)
            : base(webSocket, envelopeSerializer, traceWriter, bufferSize, webSocketMessageType)
        {
            _context = context;
            _executionTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public override IReadOnlyDictionary<string, object> Options
        {
            get
            {
                var options = (Dictionary<string, object>)base.Options;
                options["Connection.Id"] = _context.Connection.Id;
                options["Connection.LocalPort"] = _context.Connection.LocalPort;
                options["Connection.RemotePort"] = _context.Connection.RemotePort;
                options["Connection.LocalIpAddress"] = _context.Connection.LocalIpAddress;
                options["Connection.RemoteIpAddress"] = _context.Connection.RemoteIpAddress;

                if (_context.Request.Headers != null)
                {
                    foreach (var key in _context.Request.Headers.Keys.Where(o => !options.ContainsKey(o)))
                    {
                        options[$"Request.Headers.{key}"] = _context.Request.Headers[key];                        
                    }
                }

                if (_context.Request.Cookies != null)
                {
                    foreach (var key in _context.Request.Cookies.Keys.Where(o => !options.ContainsKey(o)))
                    {
                        options[$"Request.Cookies.{key}"] = _context.Request.Cookies[key];                        
                    }
                }

                return options;
            }
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken) => Task.CompletedTask;

        internal Task Execution => _executionTaskCompletionSource.Task;

        protected override async Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                await base.PerformCloseAsync(cancellationToken);
            }
            finally
            {
                _executionTaskCompletionSource.TrySetResult(null);                
            }                        
        }

        protected override void Dispose(bool disposing)
        {            
            if (disposing)
            {
                _executionTaskCompletionSource.TrySetResult(null);
            }
            
            base.Dispose(disposing);
        }
    }
}