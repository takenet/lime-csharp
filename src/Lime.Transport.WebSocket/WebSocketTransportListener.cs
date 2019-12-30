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

namespace Lime.Transport.WebSocket
{
    /// <summary>
    /// Implements a <see cref="ITransportListener"/> that uses Kestrel for receiving websocket connections.
    /// </summary>
    public class WebSocketTransportListener : ITransportListener
    {
        public static readonly string UriSchemeWebSocket = "ws";
        public static readonly string UriSchemeWebSocketSecure = "wss";
        
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly int _bufferSize;
        private readonly int _acceptCapacity;
        private readonly WebSocketMessageType _webSocketMessageType;
        private readonly ArrayPool<byte> _arrayPool;
        private readonly bool _closeGracefully;
        private readonly IWebHost _webHost;
        private Channel<ITransport> _transportChannel;

        public WebSocketTransportListener(Uri[] listenerUris,
            IEnvelopeSerializer envelopeSerializer,
            X509Certificate2 tlsCertificate = null,
            ITraceWriter traceWriter = null,
            int bufferSize = WebSocketTransport.DEFAULT_BUFFER_SIZE,
            TimeSpan? keepAliveInterval = null,
            int acceptCapacity = -1,
            HttpProtocols httpProtocols = HttpProtocols.Http1AndHttp2,
            SslProtocols sslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12,
            WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text,
            ArrayPool<byte> arrayPool = null,
            bool closeGracefully = true,
            Func<X509Certificate2, X509Chain, SslPolicyErrors, bool> clientCertificateValidationCallback = null)
        {
            if (listenerUris == null) throw new ArgumentNullException(nameof(listenerUris));
            if (listenerUris.Length == 0)
            {
                throw new ArgumentException("At least one listener URI should be provided.", nameof(listenerUris));
            }
            if (listenerUris.Any(u => u.Scheme != UriSchemeWebSocket && u.Scheme != UriSchemeWebSocketSecure))
            {
                throw new ArgumentException($"Invalid URI scheme. Should be '{UriSchemeWebSocket}' or '{UriSchemeWebSocketSecure}'.", nameof(listenerUris));
            }
            if (tlsCertificate == null && listenerUris.Any(u => u.Scheme == UriSchemeWebSocketSecure))            
            {
                throw new ArgumentException($"The certificate should be provided when listening to a '{UriSchemeWebSocketSecure}' URI.", nameof(listenerUris));
            }

            ListenerUris = listenerUris;
            _envelopeSerializer = envelopeSerializer;
            _traceWriter = traceWriter;
            _bufferSize = bufferSize;
            _acceptCapacity = acceptCapacity;
            _webSocketMessageType = webSocketMessageType;
            _arrayPool = arrayPool;
            _closeGracefully = closeGracefully;
            _webHost = new WebHostBuilder()
                .UseKestrel(serverOptions =>
                {                    
                    foreach (var listenerUri in ListenerUris)
                    {
                        if (!IPAddress.TryParse(listenerUri.Host, out var ipAddress))
                        {
                            ipAddress = IPAddress.Any;
                        }
                        
                        var endPoint = new IPEndPoint(ipAddress, listenerUri.Port);                        
                        serverOptions.Listen(endPoint, listenOptions =>
                        {                                                        
                            listenOptions.Protocols = httpProtocols;

                            if (listenerUri.Scheme == UriSchemeWebSocketSecure)
                            {                                                                
                                listenOptions.UseHttps(tlsCertificate, httpsOptions =>
                                {
                                    httpsOptions.SslProtocols = sslProtocols;
                                    httpsOptions.ClientCertificateValidation = clientCertificateValidationCallback;
                                });
                            }
                        });
                    }
                    
                    serverOptions.AddServerHeader = false;
                })                
                .SuppressStatusMessages(true)                
                .Configure(app =>
                {
                    // Add the WebSockets middleware
                    app.UseWebSockets(
                        new WebSocketOptions()
                        {
                            KeepAliveInterval = keepAliveInterval ?? System.Net.WebSockets.WebSocket.DefaultKeepAliveInterval,
                            ReceiveBufferSize = bufferSize,
                        });
                                        
                    app.Run(ProcessHttpContextAsync);
                })
                .Build();
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _transportChannel = _acceptCapacity > 0
                ? Channel.CreateBounded<ITransport>(_acceptCapacity)
                : Channel.CreateUnbounded<ITransport>();
            
            return _webHost.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (!IsStarted)
            {
                return Task.FromException(new InvalidOperationException("The listener is not started"));
            }
                        
            _transportChannel.Writer.Complete();
            
            return _webHost.StopAsync(cancellationToken);
        }

        public Uri[] ListenerUris { get; }
        
        public Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
        {
            if (!IsStarted)
            {
                return Task.FromException<ITransport>(new InvalidOperationException("The listener is not started"));
            }
            
            return _transportChannel.Reader.ReadAsync(cancellationToken).AsTask();
        }

        private bool IsStarted => _transportChannel != null && !_transportChannel.Reader.Completion.IsCompleted;
        
        private async Task ProcessHttpContextAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }                       

            var webSocket = await context.WebSockets.AcceptWebSocketAsync(LimeUri.LIME_URI_SCHEME);
            var transport = new ServerWebSocketTransport(
                context, webSocket, _envelopeSerializer, _traceWriter, _bufferSize, _webSocketMessageType,_arrayPool, _closeGracefully);

            await _transportChannel.Writer.WriteAsync(transport);

            // We should await here until the websocket is being used
            await transport.OpenTask;
        }
    }
}