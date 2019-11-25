using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using System.Buffers;

namespace Lime.Transport.Tcp
{
    public class PipeTcpTransportListener : ITransportListener
    {
        private readonly X509Certificate2 _serverCertificate;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly int _pauseWriterThreshold;
        private readonly MemoryPool<byte> _memoryPool;
        private readonly ITraceWriter _traceWriter;
        private readonly RemoteCertificateValidationCallback _clientCertificateValidationCallback;
        private readonly SemaphoreSlim _semaphore;
        private TcpListener _tcpListener;

        /// <summary>
        /// Initializes a new instance of <see cref="PipeTcpTransportListener"/> class.
        /// </summary>
        /// <param name="listenerUri">The URI for listening new connections.</param>
        /// <param name="serverCertificate">The certificate to encrypt the connections with TLS.</param>
        /// <param name="envelopeSerializer">The serializer for envelopes.</param>
        /// <param name="pauseWriterThreshold">Number of buffered bytes in the pipe which can lead the write task to pause.</param>
        /// <param name="memoryPool">The memory pool instance which allow the pipe to reuse buffers.</param>
        /// <param name="traceWriter"></param>
        /// <param name="clientCertificateValidationCallback"></param>
        public PipeTcpTransportListener(
            Uri listenerUri,
            X509Certificate2 serverCertificate,
            IEnvelopeSerializer envelopeSerializer,
            int pauseWriterThreshold = EnvelopePipe.DEFAULT_PAUSE_WRITER_THRESHOLD,
            MemoryPool<byte> memoryPool = null,
            ITraceWriter traceWriter = null,
            RemoteCertificateValidationCallback clientCertificateValidationCallback = null)
        {
            if (listenerUri == null) throw new ArgumentNullException(nameof(listenerUri));
            if (listenerUri.Scheme != TcpTransport.UriSchemeNetTcp)
            {
                throw new ArgumentException($"Invalid URI scheme. The expected value is '{TcpTransport.UriSchemeNetTcp}'.");
            }
            ListenerUris = new[] { listenerUri };
            if (serverCertificate != null
                && !serverCertificate.HasPrivateKey)
            {
                throw new ArgumentException("The certificate must have a private key", nameof(serverCertificate));
            }
            _serverCertificate = serverCertificate;
            _envelopeSerializer = envelopeSerializer ?? throw new ArgumentNullException(nameof(envelopeSerializer));
            _pauseWriterThreshold = pauseWriterThreshold;
            _memoryPool = memoryPool ?? MemoryPool<byte>.Shared;
            _traceWriter = traceWriter;
            _clientCertificateValidationCallback = clientCertificateValidationCallback;
            _semaphore = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Gets the transport 
        /// listener URIs.
        /// </summary>
        public Uri[] ListenerUris { get; }

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
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
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
            
            return new PipeTcpTransport(
                new TcpClientAdapter(tcpClient),
                _envelopeSerializer,
                _serverCertificate,
                _pauseWriterThreshold,
                _memoryPool,
                _traceWriter,
                _clientCertificateValidationCallback);
        }

        /// <summary>
        /// Stops the tranport listener
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
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
    }
}
