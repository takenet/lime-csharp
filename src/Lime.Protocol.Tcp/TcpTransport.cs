using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Tcp
{
    /// <summary>
    /// Provides the messaging protocol 
    /// transport for TCP connections
    /// </summary>
    public class TcpTransport : TransportBase, ITransport
    {
        public const int DEFAULT_BUFFER_SIZE = 8192;

        #region Private fields

        private ITcpClient _tcpClient;
        private Stream _stream;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private X509Certificate _serverCertificate;
        private string _hostName;

        #endregion

        #region Constructor

        /// <summary>
        /// Server constructor
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="envelopeSerializer"></param>
        /// <param name="serverCertificate"></param>
        /// <param name="bufferSize"></param>
        /// <param name="traceWriter"></param>
        public TcpTransport(ITcpClient tcpClient, IEnvelopeSerializer envelopeSerializer, X509Certificate serverCertificate, int bufferSize = DEFAULT_BUFFER_SIZE, ITraceWriter traceWriter = null)
            : this(tcpClient, envelopeSerializer, serverCertificate, null, bufferSize, traceWriter)
        {

        }

        public TcpTransport(ITcpClient tcpClient, IEnvelopeSerializer envelopeSerializer, string hostName, int bufferSize = DEFAULT_BUFFER_SIZE, ITraceWriter traceWriter = null)
            : this(tcpClient, envelopeSerializer, null, hostName, bufferSize, traceWriter)

        {

        }

        private TcpTransport(ITcpClient tcpClient, IEnvelopeSerializer envelopeSerializer, X509Certificate serverCertificate, string hostName, int bufferSize, ITraceWriter traceWriter)
        {
            if (tcpClient == null)
            {
                throw new ArgumentNullException("tcpClient");
            }

            _tcpClient = tcpClient;

            _buffer = new byte[bufferSize];
            _bufferPos = 0;

            if (envelopeSerializer == null)
            {
                throw new ArgumentNullException("envelopeSerializer");
            }

            _envelopeSerializer = envelopeSerializer;
            _serverCertificate = serverCertificate;
            _hostName = hostName;
            _traceWriter = traceWriter;
        }

        #endregion

        #region TransportBase Members

        /// <summary>
        /// Opens the transport connection with
        /// the specified Uri and begins to 
        /// read from the stream
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public async override Task OpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_tcpClient.Connected)
            {
                if (uri.Scheme != Uri.UriSchemeNetTcp)
                {
                    throw new ArgumentException(string.Format("Invalid URI scheme. Expected is '{0}'.", Uri.UriSchemeNetTcp));
                }

                await _tcpClient.ConnectAsync(uri.Host, uri.Port).ConfigureAwait(false);
            }

            _stream = _tcpClient.GetStream();
        }

        /// <summary>
        /// Sends an envelope to 
        /// the connected node
        /// </summary>
        /// <param name="envelope">Envelope to be transported</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException("envelope");
            }

            if (_stream == null ||
                !_stream.CanWrite)
            {
                throw new InvalidOperationException("Invalid stream state. Call OpenAsync first.");
            }

            var envelopeJson = _envelopeSerializer.Serialize(envelope);

            if (_traceWriter != null &&
                _traceWriter.IsEnabled)
            {
                await _traceWriter.TraceAsync(envelopeJson, DataOperation.Send).ConfigureAwait(false);
            }

            var jsonBytes = Encoding.UTF8.GetBytes(envelopeJson);
            await _stream.WriteAsync(jsonBytes, 0, jsonBytes.Length, cancellationToken).ConfigureAwait(false);
        }

        private SemaphoreSlim _receiveSemaphore = new SemaphoreSlim(1);
       
        /// <summary>
        /// Reads one envelope from the stream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            await _receiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                Envelope envelope = null;

                while (envelope == null && _stream.CanRead)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    byte[] json;

                    if (this.TryExtractJsonFromBuffer(out json))
                    {
                        var jsonString = Encoding.UTF8.GetString(json);

                        if (_traceWriter != null &&
                            _traceWriter.IsEnabled)
                        {
                            await _traceWriter.TraceAsync(jsonString, DataOperation.Receive).ConfigureAwait(false);
                        }

                        envelope = _envelopeSerializer.Deserialize(jsonString);
                    }

                    if (envelope == null &&
                        _stream.CanRead)
                    {
                        _bufferPos += await _stream.ReadAsync(_buffer, _bufferPos, _buffer.Length - _bufferPos, cancellationToken).ConfigureAwait(false);

                        if (_bufferPos >= _buffer.Length)
                        {
                            _stream.Close();

                            var ex = new InternalBufferOverflowException("Maximum buffer size reached");
                            await OnFailedAsync(ex).ConfigureAwait(false);
                            throw ex;
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
        /// Closes the transport
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            if (_stream != null)
            {
                _stream.Close();
            }

            _tcpClient.Close();
            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Enumerates the supported encryption
        /// options for the transport
        /// </summary>
        /// <returns></returns>
        public override SessionEncryption[] GetSupportedEncryption()
        {
            // Server or client mode
            if (_serverCertificate != null || 
                string.IsNullOrWhiteSpace(_hostName))
            {
                return new SessionEncryption[]
                {
                    SessionEncryption.None,
                    SessionEncryption.TLS
                };
            }
            else
            {
                return new SessionEncryption[]
                {
                    SessionEncryption.None
                };
            }
        }

        /// <summary>
        /// Defines the encryption mode
        /// for the transport
        /// </summary>
        /// <param name="encryption"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public override async Task SetEncryptionAsync(SessionEncryption encryption, CancellationToken cancellationToken)
        {
            switch (encryption)
            {
                case SessionEncryption.None:
                    _stream = _tcpClient.GetStream();
                    break;
                case SessionEncryption.TLS:
                    if (_serverCertificate != null)
                    {                       
                        // Server
                        var sslStream = new SslStream(
                            _stream,
                            false);

                        await sslStream
                            .AuthenticateAsServerAsync(
                                _serverCertificate,
                                false,
                                SslProtocols.Tls,
                                false)
                            .ConfigureAwait(false);

                        _stream = sslStream;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(_hostName))
                        {
                            throw new InvalidOperationException("The hostname is mandatory for TLS client encryption support");
                        }

                        // Client
                        var sslStream = new SslStream(
                            _stream,
                            false,
                             new RemoteCertificateValidationCallback(ValidateServerCertificate),
                             null
                            );

                        await sslStream
                            .AuthenticateAsClientAsync(
                                _hostName)
                            .ConfigureAwait(false);

                        _stream = sslStream;
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }

            this.Encryption = encryption;

        }

        #endregion

        #region Private methods

        private static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        #region Buffer fields

        private int _bufferPos;
        private byte[] _buffer;
        private int _jsonStartPos;
        private int _jsonPos;
        private int _jsonStackedBrackets;
        private bool _jsonStarted = false;

        #endregion

        /// <summary>
        /// Try to extract a JSON document
        /// from the buffer, based on the 
        /// brackets count.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private bool TryExtractJsonFromBuffer(out byte[] json)
        {
            if (_bufferPos > _buffer.Length)
            {
                throw new ArgumentException("Pos or length value is invalid");
            }

            int jsonLenght = 0;           

            for (int i = _jsonPos; i < _bufferPos; i++)
            {
                _jsonPos = i;

                if (_buffer[i] == '{')
                {
                    _jsonStackedBrackets++;
                    if (!_jsonStarted)
                    {
                        _jsonStartPos = i;
                        _jsonStarted = true;
                    }
                }
                else if (_buffer[i] == '}')
                {
                    _jsonStackedBrackets--;
                }

                if (_jsonStarted &&
                    _jsonStackedBrackets == 0)
                {
                    jsonLenght = i - _jsonStartPos + 1;
                    break;
                }                
            }

            json = null;

            if (jsonLenght > 1)
            {
                json = new byte[jsonLenght];
                Array.Copy(_buffer, _jsonStartPos, json, 0, jsonLenght);

                // Shifts the buffer to the left
                _bufferPos -= (jsonLenght + _jsonStartPos);
                Array.Copy(_buffer, jsonLenght + _jsonStartPos, _buffer, 0, _bufferPos);

                _jsonPos = 0;
                _jsonStartPos = 0;
                _jsonStarted = false;

                return true;
            }

            return false;
        }

        #endregion        
    }
}
