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
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;

        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private Stream _stream;

        private X509Certificate _sslCertificate;
        private string _hostName;


        const int DEFAULT_BUFFER_SIZE = 8192;

        private Task _readTask;

        public TcpTransport(TcpClient tcpClient, IEnvelopeSerializer envelopeSerializer, X509Certificate sslCertificate = null, string hostName = null, int bufferSize = DEFAULT_BUFFER_SIZE, ITraceWriter traceWriter = null)
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
            _sslCertificate = sslCertificate;
            _hostName = hostName;
            _traceWriter = traceWriter;
        }

        #region TransportBase Members

        /// <summary>
        /// Sends an envelope to 
        /// the connected node
        /// </summary>
        /// <param name="envelope">Envelope to be transported</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            var envelopeJson = _envelopeSerializer.Serialize(envelope);

            if (_traceWriter != null &&
                _traceWriter.IsEnabled)
            {
                _traceWriter.TraceAsync(envelopeJson, DataOperation.Send);
            }

            var jsonBytes = Encoding.UTF8.GetBytes(envelopeJson);
            return _stream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
        }

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
                await _tcpClient.ConnectAsync(uri.Host, uri.Port).ConfigureAwait(false);
            }

            _networkStream = _tcpClient.GetStream();

            if (!_networkStream.CanRead || !_networkStream.CanWrite)
            {
                throw new ArgumentException("Invalid stream state");
            }

            _stream = _networkStream;

            _readTask = this.ReadAsync(CancellationToken.None)
                .ContinueWith(t =>
                {
                    // In case of an uncatch exception
                    if (t.Exception != null)
                    {
                        return OnFailedAsync(t.Exception.InnerException);
                    }
                    else
                    {
                        return Task.FromResult<object>(null);
                    }
                })
                .Unwrap();
        }

        /// <summary>
        /// Closes the transport
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            _stream.Close();
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
            return new SessionEncryption[]
            {
                SessionEncryption.None,
                SessionEncryption.TLS
            };
        }

        /// <summary>
        /// Defines the encryption mode
        /// for the transport
        /// </summary>
        /// <param name="encryption"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public override Task SetEncryptionAsync(SessionEncryption encryption, CancellationToken cancellationToken)
        {
            this.Encryption = encryption;

            switch (encryption)
            {
                case SessionEncryption.None:
                    _stream = _networkStream;
                    break;
                case SessionEncryption.TLS:
                    if (_sslCertificate != null)
                    {
                        // Server
                        var sslStream = new SslStream(
                            _networkStream,
                            false);

                        return sslStream
                            .AuthenticateAsServerAsync(
                                _sslCertificate,
                                false,
                                SslProtocols.Tls,
                                false)
                            .ContinueWith(t => 
                                {
                                    if (t.Exception != null)
                                    {
                                        return OnFailedAsync(t.Exception.InnerException);
                                    }
                                    else
                                    {
                                        _stream = sslStream;
                                        return Task.FromResult<object>(null);
                                    }
                                }).Unwrap();                        
                    }
                    else
                    {
                        // Client
                        var sslStream = new SslStream(
                            _networkStream,
                            false,
                             new RemoteCertificateValidationCallback(ValidateServerCertificate),
                             null
                            );

                        return sslStream
                            .AuthenticateAsClientAsync(
                                _hostName)                                
                            .ContinueWith(t => 
                            {
                                if (t.Exception != null)
                                {
                                    return OnFailedAsync(t.Exception.InnerException);
                                }
                                else
                                {
                                    _stream = sslStream;
                                    return Task.FromResult<object>(null);
                                }
                            }).Unwrap();    

                    }

                default:
                    throw new NotSupportedException();
            }

            return Task.FromResult<object>(null);
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

        /// <summary>
        /// Reads from the stream while
        /// connected
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            while (_stream.CanRead)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var envelope = await ReadEnvelopeAsync(cancellationToken).ConfigureAwait(false);

                if (envelope != null)
                {
                    base.OnEnvelopeReceived(envelope);
                }                
            }            
        }

        /// <summary>
        /// Reads one envelope from the stream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Envelope> ReadEnvelopeAsync(CancellationToken cancellationToken)
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
                        _traceWriter.TraceAsync(jsonString, DataOperation.Receive);
                    }

                    envelope = _envelopeSerializer.Deserialize(jsonString);                    
                }

                if (envelope == null && _stream.CanRead)
                {
                    _bufferPos += await _stream.ReadAsync(_buffer, _bufferPos, _buffer.Length - _bufferPos, cancellationToken).ConfigureAwait(false);

                    if (_bufferPos >= _buffer.Length)
                    {
                        _stream.Close();

                        var exception = new InvalidOperationException("Maximum buffer size reached");
                        await OnFailedAsync(exception).ConfigureAwait(false);
                    }
                }
            }

            return envelope;
        }

        private int _bufferPos;
        private byte[] _buffer;
        private int _jsonStartPos;
        private int _jsonPos;
        private int _jsonStackedBrackets;
        private bool _jsonStarted = false;

        /// <summary>
        /// Try to extract a JSON document
        /// from the buffer
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
