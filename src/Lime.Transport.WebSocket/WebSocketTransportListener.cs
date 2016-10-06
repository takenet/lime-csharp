using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;

namespace Lime.Transport.WebSocket
{
    public class WebSocketTransportListener : ITransportListener
    {
        public static readonly string UriSchemeWebSocket = "ws";
        public static readonly string UriSchemeWebSocketSecure = "wss";

        private readonly X509Certificate2 _sslCertificate;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly int _bufferSize;
        private readonly TimeSpan _keepAliveInterval;
        private readonly HttpListener _httpListener;

        public WebSocketTransportListener(
            Uri listenerUri, 
            X509Certificate2 sslCertificate,
            IEnvelopeSerializer envelopeSerializer, 
            ITraceWriter traceWriter = null,
            int bufferSize = 16384,
            TimeSpan? keepAliveInterval = null)
        {
            if (listenerUri == null) throw new ArgumentNullException(nameof(listenerUri));

            if (listenerUri.Scheme != UriSchemeWebSocket &&
                listenerUri.Scheme != UriSchemeWebSocketSecure)
            {
                throw new ArgumentException($"Invalid URI scheme. The expected value is '{UriSchemeWebSocket}' or '{UriSchemeWebSocketSecure}'.");
            }

            if (sslCertificate == null &&
                listenerUri.Scheme == UriSchemeWebSocketSecure)
            {
                throw new ArgumentNullException(nameof(sslCertificate));
            }

            ListenerUris = new[] { listenerUri };

            if (sslCertificate != null)
            {
                if (!sslCertificate.HasPrivateKey) throw new ArgumentException("The certificate must have a private key", nameof(sslCertificate));

                try
                {
                    // Checks if the private key is available for the current user
                    var key = sslCertificate.PrivateKey;
                }
                catch (CryptographicException ex)
                {
                    throw new SecurityException("The current user doesn't have access to the certificate private key. Use WinHttpCertCfg.exe to assign the necessary permissions.", ex);
                }
            }

            if (envelopeSerializer == null)
            {
                throw new ArgumentNullException(nameof(envelopeSerializer));
            }
            _sslCertificate = sslCertificate;
            _envelopeSerializer = envelopeSerializer;
            _traceWriter = traceWriter;
            _bufferSize = bufferSize;
            _keepAliveInterval = keepAliveInterval ?? System.Net.WebSockets.WebSocket.DefaultKeepAliveInterval;
            _httpListener = new HttpListener();
        }

        public Uri[] ListenerUris { get; }
        public async Task StartAsync()
        {
            if (_httpListener.IsListening) throw new InvalidOperationException("The listener is already active");
            var prefix = ListenerUris[0].ToString();
            prefix = prefix
                .Replace($"{UriSchemeWebSocket}:", $"{Uri.UriSchemeHttp}:")
                .Replace($"{UriSchemeWebSocketSecure}:", $"{Uri.UriSchemeHttps}:")
                .Replace("://localhost", "://*");
            _httpListener.Prefixes.Add(prefix);
            _httpListener.Start();
        }

        public async Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var httpContext = await _httpListener
                        .GetContextAsync()
                        .WithCancellation(cancellationToken)
                        .ConfigureAwait(false);

                    if (httpContext.Request.IsWebSocketRequest)
                    {
                        var context = await httpContext.AcceptWebSocketAsync(
                            LimeUri.LIME_URI_SCHEME, _bufferSize, _keepAliveInterval)
                            .WithCancellation(cancellationToken)
                            .ConfigureAwait(false);
                        return new ServerWebSocketTransport(context, _envelopeSerializer, _traceWriter, _bufferSize);
                    }
                    httpContext.Response.StatusCode = 400;
                    httpContext.Response.Close();
                }
                catch (HttpListenerException ex)
                {
                    if (ex.ErrorCode == 995)
                    {
                        // Workarround since the GetContextAsync method doesn't supports cancellation
                        // "The I/O operation has been aborted because of either a thread exit or an application request"
                        throw new OperationCanceledException("The listener was canceled", ex);
                    }

                    throw;
                }
            }

            throw new OperationCanceledException();
        }

        public Task StopAsync()
        {
            _httpListener.Stop();
            return Task.CompletedTask;
        }
    }
}
