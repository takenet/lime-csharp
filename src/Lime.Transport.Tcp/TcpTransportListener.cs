using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;

namespace Lime.Transport.Tcp
{
    public class TcpTransportListener : ITransportListener
    {
        #region Private Fields
        
        private readonly X509Certificate2 _sslCertificate;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly RemoteCertificateValidationCallback _clientCertificateValidationCallback;
        private readonly SemaphoreSlim _semaphore;
        private TcpListener _tcpListener;        

        #endregion

        #region Constructor

        public TcpTransportListener(Uri listenerUri, X509Certificate2 sslCertificate, IEnvelopeSerializer envelopeSerializer, ITraceWriter traceWriter = null, RemoteCertificateValidationCallback clientCertificateValidationCallback = null)
        {
            if (listenerUri == null)
            {
                throw new ArgumentNullException("listenerUri");
            }

            if (listenerUri.Scheme != Uri.UriSchemeNetTcp)
            {
                throw new ArgumentException("Invalid URI scheme. The expected value is 'net.tcp'.");
            }

            ListenerUris = new[] { listenerUri };

            if (sslCertificate != null)
            {
                if (!sslCertificate.HasPrivateKey)
                {
                    throw new ArgumentException("The certificate must have a private key");
                }

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
                throw new ArgumentNullException("envelopeSerializer");
            }

            _sslCertificate = sslCertificate;
            _envelopeSerializer = envelopeSerializer;
            _traceWriter = traceWriter;
            _clientCertificateValidationCallback = clientCertificateValidationCallback;
            _semaphore = new SemaphoreSlim(1);
        }

        #endregion

        #region ITransportListener Members

        /// <summary>
        /// Gets the transport 
        /// listener URIs.
        /// </summary>
        public Uri[] ListenerUris { get; private set; }

        /// <summary>
        /// Start listening connections.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">listenerUri</exception>
        /// <exception cref="System.ArgumentException">
        /// Invalid URI scheme. The expected value is 'net.tcp'.
        /// or
        /// Could not resolve the IPAddress of the hostname
        /// </exception>
        /// <exception cref="System.InvalidOperationException">The listener is already active</exception>
        public async Task StartAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_tcpListener != null)
                {
                    throw new InvalidOperationException("The listener is already active");
                }

                var listenerUri = ListenerUris[0];

                IPEndPoint listenerEndPoint;
                if (listenerUri.IsLoopback)
                {
                    listenerEndPoint = new IPEndPoint(IPAddress.Any, listenerUri.Port);
                }
                else
                    switch (listenerUri.HostNameType)
                    {
                        case UriHostNameType.Dns:
                            var dnsEntry = await Dns.GetHostEntryAsync(listenerUri.Host).ConfigureAwait(false);
                            if (dnsEntry.AddressList.Any(a => a.AddressFamily == AddressFamily.InterNetwork))
                            {
                                listenerEndPoint = new IPEndPoint(dnsEntry.AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork), listenerUri.Port);
                            }
                            else
                            {
                                throw new ArgumentException(string.Format("Could not resolve the IPAddress for the host '{0}'", listenerUri.Host));
                            }
                            break;
                        case UriHostNameType.IPv4:
                        case UriHostNameType.IPv6:
                            listenerEndPoint = new IPEndPoint(IPAddress.Parse(listenerUri.Host), listenerUri.Port);
                            break;
                        default:
                            throw new ArgumentException(string.Format("The host name type for '{0}' is not supported", listenerUri.Host));
                    }

                _tcpListener = new TcpListener(listenerEndPoint);
                _tcpListener.Start();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Accepts a new transport connection
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">The listener was not started. Calls StartAsync first.</exception>
        public async Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
        {
            if (_tcpListener == null)
            {
                throw new InvalidOperationException("The listener is not active. Call StartAsync first.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var tcpClient = await _tcpListener
                .AcceptTcpClientAsync()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false);

            return new TcpTransport(
                new TcpClientAdapter(tcpClient),
                _envelopeSerializer, 
                _sslCertificate,
                clientCertificateValidationCallback: _clientCertificateValidationCallback,
                traceWriter: _traceWriter);            
        }

        /// <summary>
        /// Stops the tranport listener
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_tcpListener == null)
                {
                    throw new InvalidOperationException("The listener is not active");
                }

                _tcpListener.Stop();
                _tcpListener = null;               
            }
            finally
            {
                _semaphore.Release();
            }
        }

        #endregion
    }
}
