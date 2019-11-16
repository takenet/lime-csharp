using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using ReflectionMagic;

namespace Lime.Transport.WebSocket
{
    public class PipeClientWebSocketTransport : PipeWebSocketTransport, ITransport
    {
        public PipeClientWebSocketTransport(
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null,
            WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text,
            ClientWebSocket webSocket = null,
            bool closeGracefully = true,
            int pauseWriterThreshold = EnvelopePipe.DEFAULT_PAUSE_WRITER_THRESHOLD,
            MemoryPool<byte> memoryPool = null) 
            : base(
                webSocket ?? new ClientWebSocket(),
                envelopeSerializer,
                traceWriter,
                webSocketMessageType,
                closeGracefully,
                pauseWriterThreshold,
                memoryPool)
        {
        }

        protected override async Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            var clientWebSocket = ((ClientWebSocket) WebSocket);
            clientWebSocket.Options.AddSubProtocol(LimeUri.LIME_URI_SCHEME);
            await clientWebSocket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
            await base.PerformOpenAsync(uri, cancellationToken);
        }
        
        public override string LocalEndPoint
        {
            get
            {
                try
                {
                    // netcoreapp2.1
                    return WebSocket.AsDynamic()._innerWebSocket?._webSocket?._stream?._connection?._socket?.LocalEndPoint?.ToString();
                }
                catch
                {
                    return base.LocalEndPoint;
                }
            }
        }
        
        public override string RemoteEndPoint
        {
            get
            {
                try
                {
                    // netcoreapp2.1
                    return WebSocket.AsDynamic()._innerWebSocket?._webSocket?._stream?._connection?._socket?.RemoteEndPoint?.ToString();
                }
                catch
                {
                    return base.RemoteEndPoint;
                }
            }
        }
    }
}
