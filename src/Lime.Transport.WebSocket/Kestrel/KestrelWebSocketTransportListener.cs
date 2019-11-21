using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Lime.Transport.WebSocket.Kestrel
{
    [Obsolete("Just use WebSocketTransportListener which is Kestrel based")]
    public class KestrelWebSocketTransportListener : WebSocketTransportListener 
    {
        public KestrelWebSocketTransportListener(Uri[] listenerUris,
            IEnvelopeSerializer envelopeSerializer,
            X509Certificate2 tlsCertificate = null,
            ITraceWriter traceWriter = null,
            int bufferSize = WebSocketTransport.DEFAULT_BUFFER_SIZE,
            TimeSpan? keepAliveInterval = null,
            int acceptCapacity = -1,
            HttpProtocols httpProtocols = HttpProtocols.Http1AndHttp2,
            SslProtocols sslProtocols = SslProtocols.None | SslProtocols.Tls11 | SslProtocols.Tls12,
            WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text,
            ArrayPool<byte> arrayPool = null,
            bool closeGracefully = true,
            Func<X509Certificate2, X509Chain, SslPolicyErrors, bool> clientCertificateValidationCallback = null) 
                : base(
                    listenerUris,
                    envelopeSerializer,
                    tlsCertificate,
                    traceWriter,
                    bufferSize,
                    keepAliveInterval,
                    acceptCapacity,
                    httpProtocols,
                    sslProtocols,
                    webSocketMessageType,
                    arrayPool,
                    closeGracefully,
                    clientCertificateValidationCallback)
        {
        }
    }
}