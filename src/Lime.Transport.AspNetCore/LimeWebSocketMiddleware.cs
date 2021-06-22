using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Serialization;
using Lime.Transport.WebSocket;
using Microsoft.AspNetCore.Http;

namespace Lime.Transport.AspNetCore
{
    public class LimeWebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly TransportListener _listener;

        public LimeWebSocketMiddleware(
            RequestDelegate next,
            IEnvelopeSerializer envelopeSerializer,
            TransportListener listener)
        {
            _next = next;
            _envelopeSerializer = envelopeSerializer;
            _listener = listener;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next.Invoke(context);
                return;
            }

            var webSocket = await context.WebSockets.AcceptWebSocketAsync("lime");
            using var transport = new ServerWebSocketTransport(
                context,
                webSocket,
                _envelopeSerializer);

            try
            {
                await transport.OpenAsync(null, context.RequestAborted);
                await _listener.ListenAsync(transport, context.RequestAborted);
            }
            finally
            {
                if (transport.IsConnected)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await transport.CloseAsync(cts.Token);
                }
            }
        }
    }
}