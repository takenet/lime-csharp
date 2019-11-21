using System;
using System.Buffers;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using ReflectionMagic;

namespace Lime.Transport.WebSocket
{
    /// <summary>
    /// Implements a client websocket connection.
    /// </summary>
    public class PipeClientWebSocketTransport : PipeWebSocketTransport, ITransport
    {
        public PipeClientWebSocketTransport(
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null,
            WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text,
            ClientWebSocket webSocket = null,
            bool closeGracefully = true,
            int pauseWriterThreshold = EnvelopePipe.DEFAULT_PAUSE_WRITER_THRESHOLD,
            MemoryPool<byte> memoryPool = null, 
            X509CertificateCollection clientCertificates = null, 
            RemoteCertificateValidationCallback serverCertificateValidationCallback = null) 
            : base(
                webSocket ?? new ClientWebSocket(),
                envelopeSerializer,
                traceWriter,
                webSocketMessageType,
                closeGracefully,
                pauseWriterThreshold,
                memoryPool)
        {
            ((ClientWebSocket) WebSocket).Options.ClientCertificates = clientCertificates;
            ((ClientWebSocket) WebSocket).Options.RemoteCertificateValidationCallback = serverCertificateValidationCallback;
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
