using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using Lime.Protocol.Util;
using vtortola.WebSockets;

namespace Lime.Transport.WebSocket
{
    public class WebSocketTransportListener : ITransportListener
    {
        public static readonly string UriSchemeWebSocket = "ws";
        public static readonly string UriSchemeWebSocketSecure = "wss";

        private readonly X509Certificate2 _sslCertificate;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly SemaphoreSlim _semaphore;

        private WebSocketListener _webSocketListener;

        public WebSocketTransportListener(Uri listenerUri, X509Certificate2 sslCertificate, IEnvelopeSerializer envelopeSerializer, ITraceWriter traceWriter = null)
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
            _semaphore = new SemaphoreSlim(1);
        }

        #region ITransportListener Members

        public Uri[] ListenerUris { get; }

        public async Task StartAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_webSocketListener != null)
                {
                    throw new InvalidOperationException("The listener is already active");
                }

                var listenerUri = ListenerUris[0];

                IPEndPoint listenerEndPoint;
                if (listenerUri.IsLoopback)
                {
                    listenerEndPoint = new IPEndPoint(IPAddress.Any, listenerUri.Port);
                }
                else switch (listenerUri.HostNameType)
                {
                    case UriHostNameType.Dns:
                        var dnsEntry = await Dns.GetHostEntryAsync(listenerUri.Host).ConfigureAwait(false);
                        if (dnsEntry.AddressList.Any(a => a.AddressFamily == AddressFamily.InterNetwork))
                        {
                            listenerEndPoint =
                                new IPEndPoint(
                                    dnsEntry.AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork),
                                    listenerUri.Port);
                        }
                        else
                        {
                            throw new ArgumentException(
                                $"Could not resolve the IPAddress for the host '{listenerUri.Host}'");
                        }
                        break;
                    case UriHostNameType.IPv4:
                    case UriHostNameType.IPv6:
                        listenerEndPoint = new IPEndPoint(IPAddress.Parse(listenerUri.Host), listenerUri.Port);
                        break;
                    default:
                        throw new ArgumentException($"The host name type for '{listenerUri.Host}' is not supported");
                }

                _webSocketListener = new WebSocketListener(
                    listenerEndPoint, 
                    new WebSocketListenerOptions()
                    {
                        SubProtocols = new[] { LimeUri.LIME_URI_SCHEME }
                    });
                var rfc6455 = new vtortola.WebSockets.Rfc6455.WebSocketFactoryRfc6455(_webSocketListener);
                _webSocketListener.Standards.RegisterStandard(rfc6455);
                if (_sslCertificate != null)
                {
                    _webSocketListener.ConnectionExtensions.RegisterExtension(
                        new WebSocketSecureConnectionExtension(_sslCertificate));
                }

                _webSocketListener.Start();
            }
            finally
            {
                _semaphore.Release();
            }            
        }

        public async Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
        {
            if (_webSocketListener == null)
            {
                throw new InvalidOperationException("The listener is not active. Call StartAsync first.");
            }
            
            var webSocket = await _webSocketListener
                .AcceptWebSocketAsync(cancellationToken)
                .ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            
            return new ServerWebSocketTransport(webSocket, _envelopeSerializer, _traceWriter);
        }

        public async Task StopAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_webSocketListener == null)
                {
                    throw new InvalidOperationException("The listener is not active");
                }

                _webSocketListener.Stop();
                _webSocketListener = null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        #endregion
    }
}
