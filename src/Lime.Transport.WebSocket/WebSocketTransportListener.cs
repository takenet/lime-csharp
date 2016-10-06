using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using SslCertBinding.Net;

namespace Lime.Transport.WebSocket
{
    /// <summary>
    /// Defines a websocket transport listener service.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Server.ITransportListener" />
    public class WebSocketTransportListener : ITransportListener
    {
        public static readonly string UriSchemeWebSocket = "ws";
        public static readonly string UriSchemeWebSocketSecure = "wss";
        public static readonly Guid DefaultApplicationId = Guid.Parse("46754fc2-d8e2-4b41-a3f0-ed1878c77e59");

        private readonly X509CertificateInfo _tlsCertificate; 
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly int _bufferSize;
        private readonly bool _bindCertificateToPort;
        private readonly Guid _applicationId;
        private readonly TimeSpan _keepAliveInterval;
        private readonly HttpListener _httpListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketTransportListener"/> class.
        /// </summary>
        /// <param name="listenerUri">The listener URI.</param>
        /// <param name="tlsCertificate">The SSL/TLS certificate information to be used in case of using the 'wss' scheme.</param>
        /// <param name="envelopeSerializer">The envelope serializer.</param>
        /// <param name="traceWriter">The trace writer.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="keepAliveInterval">The keep alive interval.</param>
        /// <param name="bindCertificateToPort">if set to <c>true</c> indicates that the provided certificate should be bound to the listener IP address.</param>
        /// <param name="applicationId">The application id for binding the certificate to the listene port.</param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public WebSocketTransportListener(
            Uri listenerUri,
            X509CertificateInfo tlsCertificate,
            IEnvelopeSerializer envelopeSerializer, 
            ITraceWriter traceWriter = null,
            int bufferSize = 16384,
            TimeSpan? keepAliveInterval = null,
            bool bindCertificateToPort = true,
            Guid? applicationId = null)
        {
            if (listenerUri == null) throw new ArgumentNullException(nameof(listenerUri));

            if (listenerUri.Scheme != UriSchemeWebSocket &&
                listenerUri.Scheme != UriSchemeWebSocketSecure)
            {
                throw new ArgumentException($"Invalid URI scheme. The expected value is '{UriSchemeWebSocket}' or '{UriSchemeWebSocketSecure}'.");
            }

            if (tlsCertificate == null &&
                listenerUri.Scheme == UriSchemeWebSocketSecure)
            {
                throw new ArgumentNullException(nameof(tlsCertificate));
            }

            ListenerUris = new[] { listenerUri };
            if (envelopeSerializer == null)
            {
                throw new ArgumentNullException(nameof(envelopeSerializer));
            }
            _tlsCertificate = tlsCertificate;
            _envelopeSerializer = envelopeSerializer;
            _traceWriter = traceWriter;
            _bufferSize = bufferSize;
            _bindCertificateToPort = bindCertificateToPort;
            _applicationId = applicationId ?? DefaultApplicationId;
            _keepAliveInterval = keepAliveInterval ?? System.Net.WebSockets.WebSocket.DefaultKeepAliveInterval;
            _httpListener = new HttpListener();
        }

        public Uri[] ListenerUris { get; }

        public Task StartAsync()
        {
            if (_httpListener.IsListening) throw new InvalidOperationException("The listener is already active");
            var listenerUri = ListenerUris[0];
            var prefix = listenerUri.ToString();
            prefix = prefix
                .Replace($"{UriSchemeWebSocket}:", $"{Uri.UriSchemeHttp}:")
                .Replace($"{UriSchemeWebSocketSecure}:", $"{Uri.UriSchemeHttps}:")
                .Replace("://localhost", "://*");
            _httpListener.Prefixes.Add(prefix);

            if (_bindCertificateToPort &&
                _tlsCertificate != null &&
                listenerUri.Scheme.Equals(UriSchemeWebSocketSecure))
            {
                var ipPort = new IPEndPoint(IPAddress.Parse("0.0.0.0"), listenerUri.Port);
                var config = new CertificateBindingConfiguration();
                config.Bind(
                    new CertificateBinding(
                        _tlsCertificate.Thumbprint, _tlsCertificate.Store, ipPort, _applicationId));
            }

            _httpListener.Start();
            return Task.CompletedTask;
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
