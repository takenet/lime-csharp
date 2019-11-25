using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Microsoft.AspNetCore.Http;

namespace Lime.Transport.WebSocket
{
    public class ServerWebSocketTransport : WebSocketTransport
    {
        private readonly HttpContext _context;
        private readonly TaskCompletionSource<object> _openTcs;

        internal ServerWebSocketTransport(
            HttpContext context,
            System.Net.WebSockets.WebSocket webSocket,
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null,
            int bufferSize = 8192,
            WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text,
            ArrayPool<byte> arrayPool = null,
            bool closeGracefully = true)
            : base(webSocket, envelopeSerializer, traceWriter, bufferSize, webSocketMessageType, arrayPool, closeGracefully)
        {
            _context = context;
            _openTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public override IReadOnlyDictionary<string, object> Options
        {
            get
            {
                var options = (Dictionary<string, object>)base.Options;
                options["Connection.Id"] = _context.Connection.Id;

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

        /// <summary>
        /// Gets a task that is complete only when the transport is closed.
        /// </summary>
        internal Task OpenTask => _openTcs.Task;

        protected override async Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                await base.PerformCloseAsync(cancellationToken);
            }
            finally
            {
                _openTcs.TrySetResult(null);                
            }                        
        }

        public override string LocalEndPoint => $"{_context.Connection.LocalIpAddress}:{_context.Connection.LocalPort}";

        public override string RemoteEndPoint => $"{_context.Connection.RemoteIpAddress}:{_context.Connection.RemotePort}";

        protected override void Dispose(bool disposing)
        {            
            if (disposing)
            {
                _openTcs.TrySetResult(null);
            }
            
            base.Dispose(disposing);
        }
    }
}