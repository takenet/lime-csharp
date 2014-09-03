using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Tcp
{
	/// <summary>
	/// Provides the messaging protocol
	/// transport for TCP connections
	/// </summary>
	public class TcpTransport : TransportBase, ITransport, IAuthenticatableTransport
	{
		public const int DEFAULT_BUFFER_SIZE = 8192;

		#region Fields

		private Stream _stream;
		private readonly SemaphoreSlim _receiveSemaphore;
		private readonly SemaphoreSlim _sendSemaphore;
		private readonly ITcpClient _tcpClient;        
		private readonly IEnvelopeSerializer _envelopeSerializer;
		private readonly ITraceWriter _traceWriter;
		private readonly X509Certificate2 _serverCertificate;
		private readonly X509Certificate2 _clientCertificate;
		private string _hostName;
		
		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="TcpTransport"/> class.
		/// </summary>
		/// <param name="tcpClient">The TCP client.</param>
		/// <param name="envelopeSerializer">The envelope serializer.</param>
		/// <param name="hostName">Name of the host.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="traceWriter">The trace writer.</param>
		public TcpTransport(X509Certificate2 clientCertificate = null, int bufferSize = DEFAULT_BUFFER_SIZE, ITraceWriter traceWriter = null)
			: this(new EnvelopeSerializer(), clientCertificate, bufferSize, traceWriter)
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TcpTransport"/> class.
		/// </summary>
		/// <param name="tcpClient">The TCP client.</param>
		/// <param name="envelopeSerializer">The envelope serializer.</param>
		/// <param name="hostName">Name of the host.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="traceWriter">The trace writer.</param>
		public TcpTransport(IEnvelopeSerializer envelopeSerializer, X509Certificate2 clientCertificate = null, int bufferSize = DEFAULT_BUFFER_SIZE, ITraceWriter traceWriter = null)
			: this(new TcpClientAdapter(new TcpClient()), envelopeSerializer, null, clientCertificate, null, bufferSize, traceWriter)
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TcpTransport"/> class.
		/// </summary>
		/// <param name="tcpClient">The TCP client.</param>
		/// <param name="envelopeSerializer">The envelope serializer.</param>
		/// <param name="hostName">Name of the host.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <param name="traceWriter">The trace writer.</param>
		public TcpTransport(ITcpClient tcpClient, IEnvelopeSerializer envelopeSerializer, string hostName, X509Certificate2 clientCertificate = null, int bufferSize = DEFAULT_BUFFER_SIZE, ITraceWriter traceWriter = null)
			: this(tcpClient, envelopeSerializer, null, clientCertificate, hostName, bufferSize, traceWriter)

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
		internal TcpTransport(ITcpClient tcpClient, IEnvelopeSerializer envelopeSerializer, X509Certificate2 serverCertificate, int bufferSize = DEFAULT_BUFFER_SIZE, ITraceWriter traceWriter = null)
			: this(tcpClient, envelopeSerializer, serverCertificate, null, null, bufferSize, traceWriter)
		{

		}

		private TcpTransport(ITcpClient tcpClient, IEnvelopeSerializer envelopeSerializer, X509Certificate2 serverCertificate, X509Certificate2 clientCertificate, string hostName, int bufferSize, ITraceWriter traceWriter)
		{
			if (tcpClient == null)
			{
				throw new ArgumentNullException("tcpClient");
			}

			_tcpClient = tcpClient;

			_buffer = new byte[bufferSize];
			_bufferCurPos = 0;

			if (envelopeSerializer == null)
			{
				throw new ArgumentNullException("envelopeSerializer");
			}

			_envelopeSerializer = envelopeSerializer;

			_hostName = hostName;
			_traceWriter = traceWriter;

			_receiveSemaphore = new SemaphoreSlim(1);
			_sendSemaphore = new SemaphoreSlim(1);

			_serverCertificate = serverCertificate;
			_clientCertificate = clientCertificate;
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
				if (uri == null)
				{
					throw new ArgumentNullException("uri");
				}

				if (uri.Scheme != Uri.UriSchemeNetTcp)
				{
					throw new ArgumentException(string.Format("Invalid URI scheme. Expected is '{0}'.", Uri.UriSchemeNetTcp));
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
			await _sendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

			try
			{
				await _stream.WriteAsync(jsonBytes, 0, jsonBytes.Length, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				_sendSemaphore.Release();
			}
		}        
	   
		/// <summary>
		/// Reads one envelope from the stream
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
		{
			if (_stream == null)
			{
				throw new InvalidOperationException("The stream was not initialized. Call StartAsync first.");
			}

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
						_bufferCurPos += await _stream.ReadAsync(_buffer, _bufferCurPos, _buffer.Length - _bufferCurPos, cancellationToken).ConfigureAwait(false);

						if (_bufferCurPos >= _buffer.Length)
						{
							await base.CloseAsync(CancellationToken.None).ConfigureAwait(false);
							throw new InvalidOperationException("Maximum buffer size reached");
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

			cancellationToken.ThrowIfCancellationRequested();

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
			if (_sendSemaphore.CurrentCount == 0)
			{
				System.Console.WriteLine("Send semaphore being used");
			}

			if (_receiveSemaphore.CurrentCount == 0)
			{
				System.Console.WriteLine("Receive semaphore being used");
			}

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
							#if MONO
								// Server
								sslStream = new SslStream(
									_stream,
									false,
									new RemoteCertificateValidationCallback(ValidateClientCertificate),
									null);
							#else

								// Server
								sslStream = new SslStream(
									_stream,
									false,
									new RemoteCertificateValidationCallback(ValidateClientCertificate),
									null,
									EncryptionPolicy.RequireEncryption);
							#endif

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
									 new RemoteCertificateValidationCallback(ValidateServerCertificate));

								X509CertificateCollection clientCertificates = null;

								if (_clientCertificate != null)
								{
									clientCertificates = new X509CertificateCollection(new[] { _clientCertificate });
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

					this.Encryption = encryption;
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

		#endregion

		#region IAuthenticatableTransport Members

		/// <summary>
		/// Authenticate the identity
		/// in the transport layer
		/// </summary>
		/// <param name="identity">The identity to be authenticated</param>
		/// <returns>
		/// Indicates if the identity is authenticated
		/// </returns>
		public Task<DomainRole> AuthenticateAsync(Identity identity)
		{
			var role = DomainRole.Unknown;
			
			if (identity == null)
			{
				throw new ArgumentNullException("identity");
			}

			var sslStream = _stream as SslStream;

			if (sslStream != null &&
				sslStream.IsAuthenticated &&
				sslStream.RemoteCertificate != null)
			{                
				var certificate = new X509Certificate2(sslStream.RemoteCertificate);
				var identityCertificateName = certificate.GetNameInfo(X509NameType.SimpleName, false);

				Identity certificateIdentity;

				if (identityCertificateName.Equals(identity.Domain, StringComparison.OrdinalIgnoreCase))
				{
					role = DomainRole.Authority;
				}
				else if (Identity.TryParse(identityCertificateName, out certificateIdentity) &&
						 certificateIdentity.Equals(identity))
				{
					role = DomainRole.Member;
				}               
			}

			return Task.FromResult(role);
		}

		#endregion

		#region Private methods

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

		#region Buffer fields

		private byte[] _buffer;
		private int _bufferCurPos;
		
		private int _jsonStartPos;
		private int _jsonCurPos;
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
			if (_bufferCurPos > _buffer.Length)
			{
				throw new ArgumentException("Buffer current pos or length value is invalid");
			}

			json = null;
			int jsonLenght = 0;

			for (int i = _jsonCurPos; i < _bufferCurPos; i++)
			{
				_jsonCurPos = i + 1;

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

			if (jsonLenght > 1)
			{
				json = new byte[jsonLenght];
				Array.Copy(_buffer, _jsonStartPos, json, 0, jsonLenght);

				// Shifts the buffer to the left
				_bufferCurPos -= (jsonLenght + _jsonStartPos);
				Array.Copy(_buffer, jsonLenght + _jsonStartPos, _buffer, 0, _bufferCurPos);

				_jsonCurPos = 0;
				_jsonStartPos = 0;
				_jsonStarted = false;

				return true;
			}

			return false;
		}

		#endregion
	}
}
