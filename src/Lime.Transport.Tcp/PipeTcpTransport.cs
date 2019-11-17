using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using System.Buffers;
using System.Threading.Tasks.Dataflow;

namespace Lime.Transport.Tcp
{
    /// <summary>
    /// Provides the messaging protocol transport for TCP connections.
    /// </summary>
    public class PipeTcpTransport : TransportBase, ITransport, IAuthenticatableTransport, IDisposable
    {
        public static readonly string UriSchemeNetTcp = "net.tcp";
        public static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(30);

        private readonly SemaphoreSlim _optionsSemaphore;
        private readonly ITcpClient _tcpClient;
        private readonly RemoteCertificateValidationCallback _serverCertificateValidationCallback;
        private readonly RemoteCertificateValidationCallback _clientCertificateValidationCallback;
        private readonly X509Certificate2 _serverCertificate;
        private readonly X509Certificate2 _clientCertificate;
        private readonly EnvelopePipe _envelopePipe;
        private readonly BufferBlock<object> _readSynchronizationQueue;
        private Stream _stream;
        private string _hostName;
        private bool _disposed;
        private bool _sessionEstablished;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpTransport"/> class.
        /// </summary>
        /// <param name="clientCertificate"></param>
        /// <param name="pauseWriterThreshold">Number of buffered bytes in the pipe which can lead the write task to pause.</param>
        /// <param name="traceWriter">The trace writer.</param>
        /// <param name="serverCertificateValidationCallback">A callback to validate the server certificate in the TLS authentication process.</param>
        public PipeTcpTransport(
            X509Certificate2 clientCertificate = null,
            int pauseWriterThreshold = EnvelopePipe.DEFAULT_PAUSE_WRITER_THRESHOLD,
            ITraceWriter traceWriter = null,
            RemoteCertificateValidationCallback serverCertificateValidationCallback = null)
            : this(new EnvelopeSerializer(new DocumentTypeResolver()), clientCertificate, pauseWriterThreshold, traceWriter, serverCertificateValidationCallback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpTransport" /> class.
        /// </summary>
        /// <param name="envelopeSerializer">The envelope serializer.</param>
        /// <param name="clientCertificate">The client certificate.</param>
        /// <param name="pauseWriterThreshold">Number of buffered bytes in the pipe which can lead the write task to pause.</param>
        /// <param name="traceWriter">The trace writer.</param>
        /// <param name="serverCertificateValidationCallback">A callback to validate the server certificate in the TLS authentication process.</param>
        public PipeTcpTransport(
            IEnvelopeSerializer envelopeSerializer,
            X509Certificate2 clientCertificate = null,
            int pauseWriterThreshold = EnvelopePipe.DEFAULT_PAUSE_WRITER_THRESHOLD,
            ITraceWriter traceWriter = null,
            RemoteCertificateValidationCallback serverCertificateValidationCallback = null)
            : this(new TcpClientAdapter(new TcpClient()), envelopeSerializer, null, clientCertificate, null, pauseWriterThreshold, null, traceWriter, serverCertificateValidationCallback, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpTransport"/> class.
        /// </summary>
        /// <param name="tcpClient">The TCP client.</param>
        /// <param name="envelopeSerializer">The envelope serializer.</param>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="clientCertificate">The client certificate.</param>
        /// <param name="pauseWriterThreshold">Number of buffered bytes in the pipe which can lead the write task to pause.</param>
        /// <param name="traceWriter">The trace writer.</param>
        /// <param name="serverCertificateValidationCallback">A callback to validate the server certificate in the TLS authentication process.</param>
        public PipeTcpTransport(
            ITcpClient tcpClient,
            IEnvelopeSerializer envelopeSerializer,
            string hostName,
            X509Certificate2 clientCertificate = null,
            int pauseWriterThreshold = EnvelopePipe.DEFAULT_PAUSE_WRITER_THRESHOLD,
            ITraceWriter traceWriter = null,
            RemoteCertificateValidationCallback serverCertificateValidationCallback = null)
            : this(tcpClient, envelopeSerializer, null, clientCertificate, hostName, pauseWriterThreshold, null, traceWriter, serverCertificateValidationCallback, null)

        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpTransport"/> class.
        /// This constructor is used by the <see cref="TcpTransportListener"/> class.
        /// </summary>
        /// <param name="tcpClient">The TCP client.</param>
        /// <param name="envelopeSerializer">The envelope serializer.</param>
        /// <param name="serverCertificate">The server certificate.</param>
        /// <param name="pauseWriterThreshold">Number of buffered bytes in the pipe which can lead the write task to pause.</param>
        /// <param name="memoryPool">The memory pool instance which allow the pipe to reuse buffers.</param>
        /// <param name="traceWriter">The trace writer.</param>
        /// <param name="clientCertificateValidationCallback">A callback to validate the client certificate in the TLS authentication process.</param>
        internal PipeTcpTransport(
            ITcpClient tcpClient,
            IEnvelopeSerializer envelopeSerializer,
            X509Certificate2 serverCertificate,
            int pauseWriterThreshold = EnvelopePipe.DEFAULT_PAUSE_WRITER_THRESHOLD,
            MemoryPool<byte> memoryPool = null,
            ITraceWriter traceWriter = null,
            RemoteCertificateValidationCallback clientCertificateValidationCallback = null)
            : this(tcpClient, envelopeSerializer, serverCertificate, null, null, pauseWriterThreshold, memoryPool, traceWriter, null, clientCertificateValidationCallback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpTransport"/> class.
        /// </summary>
        /// <param name="tcpClient">The TCP client.</param>
        /// <param name="envelopeSerializer">The envelope serializer.</param>
        /// <param name="serverCertificate">The server certificate.</param>
        /// <param name="clientCertificate">The client certificate.</param>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="pauseWriterThreshold">Number of buffered bytes in the pipe which can lead the write task to pause.</param>
        /// <param name="memoryPool">The memory pool instance which allow the pipe to reuse buffers.</param>
        /// <param name="traceWriter">The trace writer.</param>
        /// <param name="serverCertificateValidationCallback">The server certificate validation callback.</param>
        /// <param name="clientCertificateValidationCallback">The client certificate validation callback.</param>
        /// <exception cref="System.ArgumentNullException">
        /// tcpClient
        /// or
        /// envelopeSerializer
        /// </exception>
        private PipeTcpTransport(
            ITcpClient tcpClient,
            IEnvelopeSerializer envelopeSerializer,
            X509Certificate2 serverCertificate,
            X509Certificate2 clientCertificate,
            string hostName,
            int pauseWriterThreshold,
            MemoryPool<byte> memoryPool,
            ITraceWriter traceWriter,
            RemoteCertificateValidationCallback serverCertificateValidationCallback,
            RemoteCertificateValidationCallback clientCertificateValidationCallback)
        {
            _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
            _serverCertificate = serverCertificate;
            _clientCertificate = clientCertificate;
            _hostName = hostName;
            _serverCertificateValidationCallback = serverCertificateValidationCallback ?? ValidateServerCertificate;
            _clientCertificateValidationCallback = clientCertificateValidationCallback ?? ValidateClientCertificate;
            _envelopePipe = new EnvelopePipe(ReceiveFromPipeAsync, SendToPipeAsync, envelopeSerializer, traceWriter, pauseWriterThreshold, memoryPool);
            _readSynchronizationQueue = new BufferBlock<object>();
            _optionsSemaphore = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Sends an envelope to the connected node.
        /// </summary>
        /// <param name="envelope">Envelope to be transported</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            UpdateSessionEstablished(envelope);

            try
            {
                await _envelopePipe.SendAsync(envelope, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                await CloseWithTimeoutAsync();
                throw;
            }
        }
        
        /// <summary>
        /// Reads one envelope from the pipe.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (CanBeUpgraded)
            {
                // Signals the pipe stream read task to proceed
                await _readSynchronizationQueue.SendAsync(null, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                var envelope = await _envelopePipe.ReceiveAsync(cancellationToken);
                UpdateSessionEstablished(envelope);
                return envelope;
            }
            catch (InvalidOperationException)
            {
                await CloseWithTimeoutAsync();
                throw;
            }
        }

        /// <summary>
        /// Enumerates the supported encryption options for the transport.
        /// </summary>
        /// <returns></returns>
        public override SessionEncryption[] GetSupportedEncryption()
        {
            // Server or client mode
            if (IsTlsSupported)
            {
                return new[]
                {
                    SessionEncryption.None,
                    SessionEncryption.TLS
                };
            }
            return new[]
            {
                SessionEncryption.None
            };
        }

        /// <summary>
        /// Defines the encryption mode for the transport.
        /// </summary>
        /// <param name="encryption"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException"></exception>        
        public override async Task SetEncryptionAsync(SessionEncryption encryption, CancellationToken cancellationToken)
        {
            await _optionsSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                switch (encryption)
                {
                    case SessionEncryption.None:
                        _stream = _tcpClient.GetStream();
                        break;

                    case SessionEncryption.TLS:
                        SslStream sslStream;
                        
                        if (_serverCertificate != null)
                        {
                            if (_stream == null) throw new InvalidOperationException("The stream was not initialized. Call OpenAsync first.");

                            // Server
                            sslStream = new SslStream(
                                _stream,
                                false,
                                _clientCertificateValidationCallback,
                                null,
                                EncryptionPolicy.RequireEncryption);

                            await sslStream
                                .AuthenticateAsServerAsync(
                                    _serverCertificate,
                                    true,
                                    SslProtocols.Tls,
                                    false)
                                .WithCancellation(cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            // Client
                            if (string.IsNullOrWhiteSpace(_hostName))
                            {
                                throw new InvalidOperationException("The hostname is mandatory for TLS client encryption support");
                            }

                            sslStream = new SslStream(
                                _stream,
                                false,
                                 _serverCertificateValidationCallback);

                            X509CertificateCollection clientCertificates = null;

                            if (_clientCertificate != null)
                            {
                                clientCertificates = new X509CertificateCollection(new X509Certificate[] { _clientCertificate });
                            }

                            await sslStream
                                .AuthenticateAsClientAsync(
                                    _hostName,
                                    clientCertificates,
                                    SslProtocols.Tls,
                                    false)
                                .WithCancellation(cancellationToken)
                                .ConfigureAwait(false);
                        }
                        _stream = sslStream;
                        break;

                    default:
                        throw new NotSupportedException();
                }

                Encryption = encryption;
            }
            finally
            {
                _optionsSemaphore.Release();
            }
        }

        /// <summary>
        /// Indicates if the transport is connected.
        /// </summary>
        public override bool IsConnected => _tcpClient.Connected;

        /// <summary>
        /// Gets the local endpoint address.
        /// </summary>
        public override string LocalEndPoint => _tcpClient.Client?.LocalEndPoint.ToString();

        /// <summary>
        /// Gets the remote endpoint address.
        /// </summary>
        public override string RemoteEndPoint => _tcpClient.Client?.RemoteEndPoint.ToString();

        /// <summary>
        /// Gets specific transport metadata information.
        /// </summary>
        public override IReadOnlyDictionary<string, object> Options
        {
            get
            {
                if (_tcpClient.Client == null) return null;
                return new Dictionary<string, object>()
                {
                    {nameof(Socket.AddressFamily), _tcpClient.Client.AddressFamily},
                    {nameof(Socket.Blocking), _tcpClient.Client.Blocking},
                    {nameof(Socket.DontFragment), _tcpClient.Client.DontFragment},
                    {nameof(Socket.ExclusiveAddressUse), _tcpClient.Client.ExclusiveAddressUse},
                    {$"{nameof(Socket.LingerState)}.{nameof(LingerOption.Enabled)}", _tcpClient.Client.LingerState?.Enabled},
                    {$"{nameof(Socket.LingerState)}.{nameof(LingerOption.LingerTime)}", _tcpClient.Client.LingerState?.LingerTime},
                    {nameof(Socket.NoDelay), _tcpClient.Client.NoDelay},
                    {nameof(Socket.ProtocolType), _tcpClient.Client.ProtocolType},
                    {nameof(Socket.ReceiveBufferSize), _tcpClient.Client.ReceiveBufferSize},
                    {nameof(Socket.ReceiveTimeout), _tcpClient.Client.ReceiveTimeout},
                    {nameof(Socket.SendBufferSize), _tcpClient.Client.SendBufferSize},
                    {nameof(Socket.SendTimeout), _tcpClient.Client.SendTimeout},
                    {nameof(Socket.SocketType), _tcpClient.Client.SocketType},
                    {nameof(Socket.Ttl), _tcpClient.Client.Ttl}
                };
            }
        }

        /// <summary>
        /// Sets a transport option value.
        /// </summary>
        /// <param name="name">Name of the option.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public override Task SetOptionAsync(string name, object value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (_tcpClient.Client == null) throw new InvalidOperationException("The client state is invalid");

            var names = name.Split('.');

            object obj;

            if (names.Length == 1)
            {
                obj = _tcpClient.Client;
            }
            else if (names.Equals(nameof(Socket.LingerState)) &&
                     names.Length == 2)
            {
                obj = _tcpClient.Client.LingerState;
                name = names[1];
            }
            else
            {
                throw new ArgumentException("Invalid option name", nameof(name));
            }

            var propertyInfo = obj.GetType().GetProperty(name);
            propertyInfo.SetValue(obj, value);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Authenticate the identity in the transport layer.
        /// </summary>
        /// <param name="identity">The identity to be authenticated</param>
        /// <returns>
        /// Indicates if the identity is authenticated
        /// </returns>
        public Task<DomainRole> AuthenticateAsync(Identity identity)
        {
            if (identity == null) throw new ArgumentNullException(nameof(identity));

            var role = DomainRole.Unknown;

            if (_stream is SslStream sslStream &&
                sslStream.IsAuthenticated &&
                sslStream.RemoteCertificate != null)
            {                
                var certificate = sslStream.RemoteCertificate;
                if (!string.IsNullOrWhiteSpace(certificate.Subject))
                {
                    var commonNames = certificate
                        .Subject
                        .Split(',')
                        .Select(c => c.Split('='))
                        .Select(c => new
                        {
                            DN = c[0].Trim(' '),
                            Subject = c[1].Trim(' ')
                        })
                        .Where(s => s.DN.Equals("CN"))
                        .ToArray();

                    Identity certificateIdentity;
                    if (
                        commonNames.Any(
                            c => c.Subject.Equals($"*.{identity.Domain}", StringComparison.OrdinalIgnoreCase)))
                    {
                        role = DomainRole.RootAuthority;
                    }
                    else if (
                        commonNames.Any(
                            c =>
                                c.Subject.Equals(identity.Domain, StringComparison.OrdinalIgnoreCase) ||
                                c.Subject.TrimStart('*', '.').Equals(identity.Domain.TrimFirstDomainLabel(), StringComparison.OrdinalIgnoreCase)))
                    {
                        role = DomainRole.Authority;
                    }
                    else if (
                        commonNames.Any(
                            c =>
                                Identity.TryParse(c.Subject, out certificateIdentity) &&
                                certificateIdentity.Equals(identity)))
                    {
                        role = DomainRole.Member;
                    }
                }
            }

            return Task.FromResult(role);
        }

        /// <summary>
        /// Opens the transport connection with the specified Uri and begins to read from the stream.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override async Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            // TODO: It is required to call OpenAsync in a server transport, which doesn't make much sense. The server transport is passive and it will be always be open after its creation.
            // We should refactor the transports to remove this need on the server side.
            if (!_tcpClient.Connected)
            {                
                if (uri == null) throw new ArgumentNullException(nameof(uri), "The uri is mandatory for a not connected TCP client");
                if (uri.Scheme != UriSchemeNetTcp)
                {
                    throw new ArgumentException($"Invalid URI scheme. Expected is '{UriSchemeNetTcp}'.", nameof(uri));
                }

                if (string.IsNullOrWhiteSpace(_hostName))
                {
                    _hostName = uri.Host;
                }

                await _tcpClient.ConnectAsync(uri.Host, uri.Port).ConfigureAwait(false);
            }

            _stream = _tcpClient.GetStream();
            await _envelopePipe.StartAsync(cancellationToken);
        }
        
        /// <summary>
        /// Closes the transport.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _stream?.Close();
            _tcpClient.Close();
            return _envelopePipe.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _optionsSemaphore.Dispose();
                    _stream?.Dispose();
                    _envelopePipe.Dispose();
                }

                _disposed = true;
            }
        }

        private ValueTask SendToPipeAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) 
            => _stream.WriteAsync(buffer, cancellationToken);

        private async ValueTask<int> ReceiveFromPipeAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (CanBeUpgraded)
            {
                // If the connection can be upgraded to TLS/Gzip, we should wait for ITransport.ReceiveAsync method to be called
                // to proceed the with the read because the _stream instance can be changed.
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                try
                {
                    await _readSynchronizationQueue.ReceiveAsync(linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested) 
                {
                    // Waits until the timeout to avoid deadlocks.
                    // This is required for the cases when is required more than 1 stream ReadAsync call for producing 1 envelope.
                }
            }
            
            return await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Indicates if the current transport instance supports TLS.
        /// </summary>
        private bool IsTlsSupported => _serverCertificate != null || !string.IsNullOrWhiteSpace(_hostName);

        /// <summary>
        /// Indicates if the transport options can be upgraded, which will change the value of the <see cref="_stream"/> field instance.
        /// </summary>
        private bool CanBeUpgraded => !_sessionEstablished && Encryption == SessionEncryption.None && IsTlsSupported;
        
        /// <summary>
        /// Sets the value of the <see cref="_sessionEstablished"/> field based on the envelopes that are exchanged.
        /// </summary>
        /// <param name="envelope"></param>
        private void UpdateSessionEstablished(Envelope envelope)
        {
            if (!_sessionEstablished && 
                (!(envelope is Session) || ((Session)envelope).State == SessionState.Established))
            {
                _sessionEstablished = true;
            }
        }        
        
        private bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return sslPolicyErrors == SslPolicyErrors.None ||
                   sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch;
        }

        private bool ValidateClientCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            // TODO: Check key usage
            // The client certificate can be null 
            // but if present, must be valid
            if (certificate == null)
            {
                return true;
            }
            return sslPolicyErrors == SslPolicyErrors.None;
        }

        private Task CloseWithTimeoutAsync()
        {
            using (var cts = new CancellationTokenSource(CloseTimeout))
            {
                return CloseAsync(cts.Token);
            }
        }
    }
}