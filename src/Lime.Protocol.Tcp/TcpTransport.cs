using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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
    public class TcpTransport : ITransport
    {
        private readonly IEnvelopeSerializer _serializer;
        private readonly ITraceWriter _traceWriter;

        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private Stream _stream;

        private X509Certificate _sslCertificate;
        private string _hostName;

        private int _pos;
        private byte[] _buffer;
        const int DEFAULT_BUFFER_SIZE = 8192;


        public TcpTransport(IEnvelopeSerializer serializer, ITraceWriter traceWriter)
        {

        }

        #region ITransport Members

        public Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<EnvelopeEventArgs<Envelope>> EnvelopeReceived;

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public SessionCompression[] GetSupportedCompression()
        {
            throw new NotImplementedException();
        }

        public SessionCompression Compression
        {
            get { throw new NotImplementedException(); }
        }

        public Task SetCompressionAsync(SessionCompression compression, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public SessionEncryption[] GetSupportedEncryption()
        {
            throw new NotImplementedException();
        }

        public SessionEncryption Encryption
        {
            get { throw new NotImplementedException(); }
        }

        public Task SetEncryptionAsync(SessionEncryption encryption, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<ExceptionEventArgs> Failed;

        public event EventHandler<DeferralEventArgs> Closing;

        public event EventHandler Closed;

        #endregion
    }
}
