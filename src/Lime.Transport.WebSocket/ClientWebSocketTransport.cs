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
    public class ClientWebSocketTransport : WebSocketTransport, ITransport
    {
        public ClientWebSocketTransport(
            IEnvelopeSerializer envelopeSerializer, 
            ITraceWriter traceWriter = null, 
            int bufferSize = DEFAULT_BUFFER_SIZE,
            WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text,
            ClientWebSocket webSocket = null,
            ArrayPool<byte> arrayPool = null,
            bool closeGracefully = true, 
            X509CertificateCollection clientCertificates = null, 
            RemoteCertificateValidationCallback serverCertificateValidationCallback = null) 
            : base(
                webSocket ?? new ClientWebSocket(), 
                envelopeSerializer, 
                traceWriter, 
                bufferSize, 
                webSocketMessageType, 
                arrayPool, 
                closeGracefully)
        {
            if (clientCertificates != null)
            {
                ((ClientWebSocket) WebSocket).Options.ClientCertificates = clientCertificates;
            }

            if (serverCertificateValidationCallback != null)
            {
                ((ClientWebSocket) WebSocket).Options.RemoteCertificateValidationCallback =
                    serverCertificateValidationCallback;
            }
        }        

        protected override async Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            var clientWebSocket = ((ClientWebSocket) WebSocket);
            clientWebSocket.Options.AddSubProtocol(LimeUri.LIME_URI_SCHEME);
            await clientWebSocket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
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
