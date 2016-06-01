using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;

namespace Lime.Transport.Tcp
{
    /// <summary>
	/// Provides the messaging protocol transport for TCP connections.
	/// </summary>
	public class TcpTransport : TransportBase, ITransport, IAuthenticatableTransport
    {
        public const int DEFAULT_BUFFER_SIZE = 8192;

        public static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(30);
        
        private readonly SemaphoreSlim _receiveSemaphore;
        private readonly SemaphoreSlim _sendSemaphore;
        private readonly ITcpClient _tcpClient;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly RemoteCertificateValidationCallback _serverCertificateValidationCallback;
        private readonly RemoteCertificateValidationCallback _clientCertificateValidationCallback;
        private readonly X509Certificate2 _serverCertificate;
        private readonly X509Certificate2 _clientCertificate;        
        private readonly JsonBuffer _jsonBuffer;

        private Stream _stream;
        private string _hostName;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpTransport"/> class.
        /// </summary>
        /// <param name="clientCertificate"></param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="traceWriter">The trace writer.</param>
        /// <param name="serverCertificateValidationCallback">A callback to validate the server certificate in the TLS authentication process.</param>
        public TcpTransport(
            X509Certificate2 clientCertificate = null, 
            int bufferSize = DEFAULT_BUFFER_SIZE, 
            ITraceWriter traceWriter = null, 
            RemoteCertificateValidationCallback serverCertificateValidationCallback = null)
            : this(new JsonNetSerializer(), clientCertificate, bufferSize, traceWriter, serverCertificateValidationCallback)
        {
        }

        /// <summary>
	    /// Initializes a new instance of the <see cref="TcpTransport"/> class.
	    /// </summary>
	    /// <param name="envelopeSerializer">The envelope serializer.</param>
	    /// <param name="clientCertificate">The client certificate.</param>
	    /// <param name="bufferSize">Size of the buffer.</param>
	    /// <param name="traceWriter">The trace writer.</param>
        /// <param name="serverCertificateValidationCallback">A callback to validate the server certificate in the TLS authentication process.</param>
	    public TcpTransport(
            IEnvelopeSerializer envelopeSerializer, 
            X509Certificate2 clientCertificate = null, 
            int bufferSize = DEFAULT_BUFFER_SIZE, 
            ITraceWriter traceWriter = null, 
            RemoteCertificateValidationCallback serverCertificateValidationCallback = null)
            : this(new TcpClientAdapter(new TcpClient()), envelopeSerializer, null, clientCertificate, null, bufferSize, traceWriter, serverCertificateValidationCallback, null)
        {
        }

        /// <summary>
	    /// Initializes a new instance of the <see cref="TcpTransport"/> class.
	    /// </summary>
	    /// <param name="tcpClient">The TCP client.</param>
	    /// <param name="envelopeSerializer">The envelope serializer.</param>
	    /// <param name="hostName">Name of the host.</param>
	    /// <param name="clientCertificate">The client certificate.</param>
	    /// <param name="bufferSize">Size of the buffer.</param>
	    /// <param name="traceWriter">The trace writer.</param>
        /// <param name="serverCertificateValidationCallback">A callback to validate the server certificate in the TLS authentication process.</param>
	    public TcpTransport(
            ITcpClient tcpClient, 
            IEnvelopeSerializer envelopeSerializer, 
            string hostName, 
            X509Certificate2 clientCertificate = null, 
            int bufferSize = DEFAULT_BUFFER_SIZE, 
            ITraceWriter traceWriter = null, 
            RemoteCertificateValidationCallback serverCertificateValidationCallback = null)
            : this(tcpClient, envelopeSerializer, null, clientCertificate, hostName, bufferSize, traceWriter, serverCertificateValidationCallback, null)

        {
        }

        /// <summary>
	    /// Initializes a new instance of the <see cref="TcpTransport"/> class.
	    /// This constructor is used by the <see cref="TcpTransportListener"/> class.
	    /// </summary>
	    /// <param name="tcpClient">The TCP client.</param>
	    /// <param name="envelopeSerializer">The envelope serializer.</param>
	    /// <param name="serverCertificate">The server certificate.</param>
	    /// <param name="bufferSize">Size of the buffer.</param>
	    /// <param name="traceWriter">The trace writer.</param>
        /// <param name="clientCertificateValidationCallback">A callback to validate the client certificate in the TLS authentication process.</param>
	    internal TcpTransport(
            ITcpClient tcpClient, 
            IEnvelopeSerializer envelopeSerializer, 
            X509Certificate2 serverCertificate, 
            int bufferSize = DEFAULT_BUFFER_SIZE, 
            ITraceWriter traceWriter = null, 
            RemoteCertificateValidationCallback clientCertificateValidationCallback = null)
            : this(tcpClient, envelopeSerializer, serverCertificate, null, null, bufferSize, traceWriter, null, clientCertificateValidationCallback)
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
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="traceWriter">The trace writer.</param>
        /// <param name="serverCertificateValidationCallback">The server certificate validation callback.</param>
        /// <param name="clientCertificateValidationCallback">The client certificate validation callback.</param>
        /// <exception cref="System.ArgumentNullException">
        /// tcpClient
        /// or
        /// envelopeSerializer
        /// </exception>
		private TcpTransport(
            ITcpClient tcpClient, 
            IEnvelopeSerializer envelopeSerializer, 
            X509Certificate2 serverCertificate, 
            X509Certificate2 clientCertificate, 
            string hostName, 
            int bufferSize, 
            ITraceWriter traceWriter, 
            RemoteCertificateValidationCallback serverCertificateValidationCallback, 
            RemoteCertificateValidationCallback clientCertificateValidationCallback)
        {
            if (tcpClient == null) throw new ArgumentNullException(nameof(tcpClient));
            if (envelopeSerializer == null) throw new ArgumentNullException(nameof(envelopeSerializer));

            _tcpClient = tcpClient;
            _jsonBuffer = new JsonBuffer(bufferSize);
            _envelopeSerializer = envelopeSerializer;
            _hostName = hostName;
            _traceWriter = traceWriter;
            _receiveSemaphore = new SemaphoreSlim(1);
            _sendSemaphore = new SemaphoreSlim(1);
            _serverCertificate = serverCertificate;
            _clientCertificate = clientCertificate;
            _serverCertificateValidationCallback = serverCertificateValidationCallback ?? ValidateServerCertificate;
            _clientCertificateValidationCallback = clientCertificateValidationCallback ?? ValidateClientCertificate;
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
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (_stream == null) throw new InvalidOperationException("The stream was not initialized. Call OpenAsync first.");
            if (!_stream.CanWrite) throw new InvalidOperationException("Invalid stream state");

            var envelopeJson = _envelopeSerializer.Serialize(envelope);

            if (_traceWriter != null &&
                _traceWriter.IsEnabled)
            {
                await _traceWriter.TraceAsync(envelopeJson, DataOperation.Send).ConfigureAwait(false);
            }

            var jsonBytes = Encoding.UTF8.GetBytes(envelopeJson);
            await _sendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await _stream.WriteAsync(jsonBytes, 0, jsonBytes.Length, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                await CloseWithTimeoutAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        /// <summary>
        /// Reads one envelope from the stream.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (_stream == null) throw new InvalidOperationException("The stream was not initialized. Call OpenAsync first.");
            if (!_stream.CanRead) throw new InvalidOperationException("Invalid stream state");

            await _receiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                Envelope envelope = null;

                while (envelope == null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    byte[] jsonBytes;

                    if (_jsonBuffer.TryExtractJsonFromBuffer(out jsonBytes))
                    {
                        var envelopeJson = Encoding.UTF8.GetString(jsonBytes);

                        if (_traceWriter != null &&
                            _traceWriter.IsEnabled)
                        {
                            await _traceWriter.TraceAsync(envelopeJson, DataOperation.Receive).ConfigureAwait(false);
                        }

                        envelope = _envelopeSerializer.Deserialize(envelopeJson);
                    }

                    if (envelope == null && CanRead)
                    {
                        // The NetworkStream ReceiveAsync method doesn't supports cancellation
                        // http://stackoverflow.com/questions/21468137/async-network-operations-never-finish
                        // http://referencesource.microsoft.com/#mscorlib/system/io/stream.cs,421
                        var readTask = _stream
                            .ReadAsync(_jsonBuffer.Buffer, _jsonBuffer.BufferCurPos, _jsonBuffer.Buffer.Length - _jsonBuffer.BufferCurPos, cancellationToken);

                        while (!readTask.IsCompleted && CanRead)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            await Task
                                .WhenAny(
                                    readTask,
                                    Task.Delay(ReadTimeout, cancellationToken))
                                .ConfigureAwait(false);
                        }

                        // If is not possible to read, closes the transport and returns
                        if (!readTask.IsCompleted)
                        {
                            await CloseWithTimeoutAsync().ConfigureAwait(false);
                            break;
                        }

                        var read = await readTask.ConfigureAwait(false);

                        // https://msdn.microsoft.com/en-us/library/hh193669(v=vs.110).aspx
                        if (read == 0)
                        {
                            await CloseWithTimeoutAsync().ConfigureAwait(false);
                            break;
                        }

                        _jsonBuffer.BufferCurPos += read;

                        if (_jsonBuffer.BufferCurPos >= _jsonBuffer.Buffer.Length)
                        {
                            await CloseWithTimeoutAsync().ConfigureAwait(false);
                            throw new BufferOverflowException("Maximum buffer size reached");
                        }
                    }
                }

                return envelope;
            }
            finally
            {
                _receiveSemaphore.Release();
            }
        }

        /// <summary>
        /// Enumerates the supported encryption options for the transport.
        /// </summary>
        /// <returns></returns>
        public override SessionEncryption[] GetSupportedEncryption()
        {
            // Server or client mode
            if (_serverCertificate != null ||
                string.IsNullOrWhiteSpace(_hostName))
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
#if DEBUG
            if (_sendSemaphore.CurrentCount == 0)
            {
                Console.WriteLine("Send semaphore being used");
            }

            if (_receiveSemaphore.CurrentCount == 0)
            {
                Console.WriteLine("Receive semaphore being used");
            }
#endif

            await _sendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _receiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
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
                    _receiveSemaphore.Release();
                }

            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        public override bool IsConnected => _tcpClient.Connected;

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

            var sslStream = _stream as SslStream;
            if (sslStream != null &&
                sslStream.IsAuthenticated &&
                sslStream.RemoteCertificate != null)
            {
                var certificate = new X509Certificate2(sslStream.RemoteCertificate);
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
                        .Where(s => s.DN.Equals("CN"));
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
            cancellationToken.ThrowIfCancellationRequested();

            if (!_tcpClient.Connected)
            {
                if (uri == null) throw new ArgumentNullException(nameof(uri), "The uri is mandatory for a not connected TCP client");
                if (uri.Scheme != Uri.UriSchemeNetTcp)
                {
                    throw new ArgumentException($"Invalid URI scheme. Expected is '{Uri.UriSchemeNetTcp}'.", nameof(uri));
                }

                if (string.IsNullOrWhiteSpace(_hostName))
                {
                    _hostName = uri.Host;
                }

                await _tcpClient.ConnectAsync(uri.Host, uri.Port).ConfigureAwait(false);
            }

            _stream = _tcpClient.GetStream();
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
            return Task.FromResult<object>(null);
        }

        private bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
#if DEBUG
            return true;
#else
			return sslPolicyErrors == SslPolicyErrors.None || 
				   sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch;
#endif
        }

        private bool ValidateClientCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
#if DEBUG
            return true;
#else
			// TODO: Check key usage

			// The client certificate can be null 
			// but if present, must be valid
			if (certificate == null)
			{
				return true;
			}
			else
			{
				return sslPolicyErrors == SslPolicyErrors.None;
			}
#endif
        }

        private bool CanRead => _stream.CanRead && _tcpClient.Connected;

        private Task CloseWithTimeoutAsync()
        {
            using (var cts = new CancellationTokenSource(CloseTimeout))
            {
                return CloseAsync(cts.Token);
            }
        }
    }
}