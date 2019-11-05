using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
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
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

namespace Lime.Transport.Tcp
{
    /// <summary>
    /// Provides the messaging protocol transport for TCP connections.
    /// </summary>
    public class PipeTcpTransport : TransportBase, ITransport, IAuthenticatableTransport, IDisposable
    {
        public const int DEFAULT_BUFFER_SIZE = 8192;
        public const int DEFAULT_MAX_BUFFER_SIZE = DEFAULT_BUFFER_SIZE * 1024;

        public static readonly string UriSchemeNetTcp = "net.tcp";
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

        private readonly Pipe _receivePipe;
        private readonly Channel<Envelope> _receiveChannel;
        private readonly CancellationTokenSource _receiveCts;
        private Task _receiveTask;
        
        private Stream _stream;
        private string _hostName;
        private bool _disposed;
        

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpTransport"/> class.
        /// </summary>
        /// <param name="clientCertificate"></param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="maxBufferSize">Max size of the buffer for increasing.</param>
        /// <param name="traceWriter">The trace writer.</param>
        /// <param name="serverCertificateValidationCallback">A callback to validate the server certificate in the TLS authentication process.</param>
        public PipeTcpTransport(
            X509Certificate2 clientCertificate = null,
            int bufferSize = DEFAULT_BUFFER_SIZE,
            int maxBufferSize = DEFAULT_MAX_BUFFER_SIZE,
            ITraceWriter traceWriter = null,
            RemoteCertificateValidationCallback serverCertificateValidationCallback = null)
            : this(new EnvelopeSerializer(new DocumentTypeResolver()), clientCertificate, bufferSize, maxBufferSize, traceWriter, serverCertificateValidationCallback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpTransport" /> class.
        /// </summary>
        /// <param name="envelopeSerializer">The envelope serializer.</param>
        /// <param name="clientCertificate">The client certificate.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="maxBufferSize">Max size of the buffer for increasing.</param>
        /// <param name="traceWriter">The trace writer.</param>
        /// <param name="serverCertificateValidationCallback">A callback to validate the server certificate in the TLS authentication process.</param>
        public PipeTcpTransport(
            IEnvelopeSerializer envelopeSerializer,
            X509Certificate2 clientCertificate = null,
            int bufferSize = DEFAULT_BUFFER_SIZE,
            int maxBufferSize = DEFAULT_MAX_BUFFER_SIZE,
            ITraceWriter traceWriter = null,
            RemoteCertificateValidationCallback serverCertificateValidationCallback = null)
            : this(new TcpClientAdapter(new TcpClient()), envelopeSerializer, null, clientCertificate, null, bufferSize, maxBufferSize, null, traceWriter, serverCertificateValidationCallback, null)
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
        /// <param name="maxBufferSize">Max size of the buffer for increasing.</param>
        /// <param name="traceWriter">The trace writer.</param>
        /// <param name="serverCertificateValidationCallback">A callback to validate the server certificate in the TLS authentication process.</param>
        public PipeTcpTransport(
            ITcpClient tcpClient,
            IEnvelopeSerializer envelopeSerializer,
            string hostName,
            X509Certificate2 clientCertificate = null,
            int bufferSize = DEFAULT_BUFFER_SIZE,
            int maxBufferSize = DEFAULT_MAX_BUFFER_SIZE,
            ITraceWriter traceWriter = null,
            RemoteCertificateValidationCallback serverCertificateValidationCallback = null)
            : this(tcpClient, envelopeSerializer, null, clientCertificate, hostName, bufferSize, maxBufferSize, null, traceWriter, serverCertificateValidationCallback, null)

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
        /// <param name="maxBufferSize">Max size of the buffer for increasing.</param>
        /// <param name="memoryPool">The array pool instance, to allow reusing the created buffers.</param>
        /// <param name="traceWriter">The trace writer.</param>
        /// <param name="clientCertificateValidationCallback">A callback to validate the client certificate in the TLS authentication process.</param>
        /// <param name="ignoreDeserializationErrors">if set to <c>true</c> the deserialization on received envelopes will be ignored; otherwise, any deserialization error will be throw to the caller.</param>
        internal PipeTcpTransport(
            ITcpClient tcpClient,
            IEnvelopeSerializer envelopeSerializer,
            X509Certificate2 serverCertificate,
            int bufferSize = DEFAULT_BUFFER_SIZE,
            int maxBufferSize = DEFAULT_MAX_BUFFER_SIZE,
            MemoryPool<byte> memoryPool = null,
            ITraceWriter traceWriter = null,
            RemoteCertificateValidationCallback clientCertificateValidationCallback = null)
            : this(tcpClient, envelopeSerializer, serverCertificate, null, null, bufferSize, maxBufferSize, memoryPool, traceWriter, null, clientCertificateValidationCallback)
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
        /// <param name="maxBufferSize">Max size of the buffer.</param>
        /// <param name="memoryPool">The array pool instance, to allow reusing the created buffers.</param>
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
            int bufferSize,
            int maxBufferSize,
            MemoryPool<byte> memoryPool,
            ITraceWriter traceWriter,
            RemoteCertificateValidationCallback serverCertificateValidationCallback,
            RemoteCertificateValidationCallback clientCertificateValidationCallback)
        {
            _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
            _envelopeSerializer = envelopeSerializer ?? throw new ArgumentNullException(nameof(envelopeSerializer));
            _serverCertificate = serverCertificate;
            _clientCertificate = clientCertificate;
            _hostName = hostName;
            _traceWriter = traceWriter;
            _serverCertificateValidationCallback = serverCertificateValidationCallback ?? ValidateServerCertificate;
            _clientCertificateValidationCallback = clientCertificateValidationCallback ?? ValidateClientCertificate;

            // TODO: Define a bound
            _receiveChannel = Channel.CreateUnbounded<Envelope>();
            _receivePipe = new Pipe(new PipeOptions(memoryPool ?? MemoryPool<byte>.Shared));
            _receiveCts = new CancellationTokenSource();
            
            _receiveSemaphore = new SemaphoreSlim(1);
            _sendSemaphore = new SemaphoreSlim(1);
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
            await TraceAsync(envelopeJson, DataOperation.Send).ConfigureAwait(false);
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
        public override Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (_stream == null)
                throw new InvalidOperationException("The stream was not initialized. Call OpenAsync first.");
            if (!_stream.CanRead) throw new InvalidOperationException("Invalid stream state");

            return _receiveChannel.Reader.ReadAsync(cancellationToken).AsTask();
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

            var sslStream = _stream as SslStream;
            if (sslStream != null &&
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


            _receiveTask = ProcessReceivedConnectionAsync(_receiveCts.Token);
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
            _receiveCts.Cancel();
            return Task.FromResult<object>(null);
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
                    _sendSemaphore.Dispose();
                    _receiveSemaphore.Dispose();
                    _stream?.Dispose();
                    _receiveCts.Dispose();
                }

                _disposed = true;
            }
        }
        
        private async Task ProcessReceivedConnectionAsync(CancellationToken cancellationToken)
        {
            var pipe = new Pipe();

            var fillPipeTask = FillPipeAsync(pipe.Writer, _stream, cancellationToken);
            var readPipeTask = ReadPipeAsync(pipe.Reader, cancellationToken);

            try
            {
                await Task.WhenAll(fillPipeTask, readPipeTask);
            }
            finally
            {
                await CloseWithTimeoutAsync().ConfigureAwait(false);
            }
        }

        private async Task FillPipeAsync(PipeWriter writer, Stream stream, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var memory = writer.GetMemory();
                var read  = await stream.ReadAsync(memory, cancellationToken);
                if (read == 0) break;
                writer.Advance(read);
                var result = await writer.FlushAsync(cancellationToken);

                if (result.IsCompleted) break;
            }
            
            await writer.CompleteAsync();
        }

        private async Task ReadPipeAsync(PipeReader reader, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                if (result.IsCompleted || buffer.IsEmpty) break;
                
                var consumed = buffer.Start;
                var unexaminedBuffer = buffer;
                
                while (!unexaminedBuffer.IsEmpty  &&
                       TryExtractJsonFromBuffer(unexaminedBuffer, out var json))
                {
                    consumed = json.End;
                    unexaminedBuffer = unexaminedBuffer.Slice(consumed, unexaminedBuffer.End);

                    var envelopeJson = Encoding.UTF8.GetString(json.ToArray());
                    await TraceAsync(envelopeJson, DataOperation.Receive).ConfigureAwait(false);
                    var envelope = _envelopeSerializer.Deserialize(envelopeJson);
                    
                    await _receiveChannel.Writer.WriteAsync(envelope, cancellationToken);
                }
                
                reader.AdvanceTo(consumed, buffer.End);
            }
        }
        
        /// <summary>
        /// Try to extract a JSON document from the buffer, based on the brackets count.
        /// </summary>
        private static bool TryExtractJsonFromBuffer(ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> json)
        {
            var jsonLength = 0;
            var jsonStackedBrackets = 0;
            var jsonStartPos = 0;
            var jsonStarted = false;
            var insideQuotes = false;
            var isEscaping = false;
            var i = 0;
            
            foreach (var segment in buffer)
            {
                foreach (var c in segment.Span)
                {
                    if (c == '"' && !isEscaping)
                    {
                        insideQuotes = !insideQuotes;
                    }

                    if (!insideQuotes)
                    {
                        if (c == '{')
                        {
                            jsonStackedBrackets++;
                            if (!jsonStarted)
                            {
                                jsonStartPos = i;
                                jsonStarted = true;
                            }
                        }
                        else if (c == '}')
                        {
                            jsonStackedBrackets--;
                        }

                        if (jsonStarted &&
                            jsonStackedBrackets == 0)
                        {
                            jsonLength = i - jsonStartPos + 1;
                            break;
                        }
                    }
                    else
                    {
                        if (isEscaping)
                        {
                            isEscaping = false;
                        }
                        else if (c == '\\')
                        {
                            isEscaping = true;
                        }
                    }
                    i++;
                }
            }
            
            if (jsonLength > 1)
            {
                json = buffer.Slice(jsonStartPos, jsonLength);
                return true;
            }
            
            json = default;
            return false;
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

        private bool CanRead => _stream != null && _stream.CanRead && _tcpClient.Connected;

        private Task CloseWithTimeoutAsync()
        {
            using (var cts = new CancellationTokenSource(CloseTimeout))
            {
                return CloseAsync(cts.Token);
            }
        }

        private Task TraceAsync(string data, DataOperation operation)
        {
            if (_traceWriter == null || !_traceWriter.IsEnabled) return Task.CompletedTask;
            return _traceWriter.TraceAsync(data, operation);
        }
    }
}