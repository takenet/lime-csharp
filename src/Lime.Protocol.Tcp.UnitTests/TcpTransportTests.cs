using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Moq;
using Lime.Protocol.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Lime.Protocol.UnitTests;
using System.Threading;
using System.Net.Sockets;
using Lime.Protocol.Network;
using System.Text;

namespace Lime.Protocol.Tcp.UnitTests
{
    [TestClass]
    public class TcpTransportTests
    {
        private Mock<ITcpClient> _tcpClient;
        private Mock<IEnvelopeSerializer> _envelopeSerializer;
        private Mock<ITraceWriter> _traceWriter;

        private Mock<Stream> _stream;

        #region Private fields

        public TcpTransportTests()
        {
            _stream = new Mock<Stream>();
            _tcpClient = new Mock<ITcpClient>();
            _tcpClient
                .Setup(c => c.GetStream())
                .Returns(() => _stream.Object);

            _envelopeSerializer = new Mock<IEnvelopeSerializer>();
            _traceWriter = new Mock<ITraceWriter>();
        }

        #endregion

        #region Private methods

        private TcpTransport GetTarget(X509Certificate certificate = null, int bufferSize = TcpTransport.DEFAULT_BUFFER_SIZE)
        {
            return new TcpTransport(
                _tcpClient.Object,
                _envelopeSerializer.Object,
                certificate,
                bufferSize,
                _traceWriter.Object);
        }
        private async Task<TcpTransport> GetTargetAndOpenAsync(int bufferSize = TcpTransport.DEFAULT_BUFFER_SIZE, Stream stream = null)
        {
            if (stream == null)
            {
                stream = _stream.Object;
            }

            var uri = DataUtil.CreateUri(Uri.UriSchemeNetTcp);
            var cancellationToken = CancellationToken.None;

            var readTcs = new TaskCompletionSource<int>();

            _tcpClient
                .Setup(c => c.Connected)
                .Returns(true)
                .Verifiable();

            _tcpClient
                .Setup(s => s.GetStream())
                .Returns(stream);

            var target = GetTarget(bufferSize: bufferSize);

            await target.OpenAsync(uri, cancellationToken);
            return target;
        }  

        #endregion

        #region OpenAsync

        [TestMethod]
        [TestCategory("OpenAsync")]
        public async Task OpenAsync_NotConnectedValidUri_ConnectsClientAndCallsGetStream()
        {
            var uri = DataUtil.CreateUri(Uri.UriSchemeNetTcp);
            var cancellationToken = CancellationToken.None;
            int bufferSize = DataUtil.CreateRandomInt(10000);

            var readTcs = new TaskCompletionSource<int>();

            _tcpClient
                .Setup(c => c.Connected)
                .Returns(false)
                .Verifiable();

            var target = GetTarget(bufferSize: bufferSize);

            await target.OpenAsync(uri, cancellationToken);

            _tcpClient.Verify();
            _stream.Verify();

            _tcpClient.Verify(
                c => c.ConnectAsync(
                    uri.Host,
                    uri.Port),
                Times.Once());

            _tcpClient.Verify(
                c => c.GetStream(),
                Times.Once());
        }

        [TestMethod]
        [TestCategory("OpenAsync")]
        [ExpectedException(typeof(ArgumentException))]
        public async Task OpenAsync_NotConnectedInvalidSchemeUri_ThrowsArgumentException()
        {
            var uri = DataUtil.CreateUri(Uri.UriSchemeHttp);
            var cancellationToken = CancellationToken.None;

            _tcpClient
                .Setup(c => c.Connected)
                .Returns(false)
                .Verifiable();

            var target = GetTarget();

            await target.OpenAsync(uri, cancellationToken);
        }

        [TestMethod]
        [TestCategory("OpenAsync")]
        public async Task OpenAsync_AlreadyConnectedValidUri_CallsGetStream()
        {
            var uri = DataUtil.CreateUri(Uri.UriSchemeNetTcp);
            var cancellationToken = CancellationToken.None;
            int bufferSize = DataUtil.CreateRandomInt(10000);

            var readTcs = new TaskCompletionSource<int>();

            _tcpClient
                .Setup(c => c.Connected)
                .Returns(true)
                .Verifiable();

            var target = GetTarget(bufferSize: bufferSize);

            await target.OpenAsync(uri, cancellationToken);

            _tcpClient.Verify();
            _stream.Verify();

            _tcpClient.Verify(
                c => c.ConnectAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>()),
                Times.Never());

            _tcpClient.Verify(
                c => c.GetStream(),
                Times.Once());
        }
    
        #endregion

        #region SendAsync

        [TestMethod]
        [TestCategory("SendAsync")]
        public async Task SendAsync_ValidArgumentsAndOpenStreamAndTraceEnabled_CallsWriteAsyncAndTraces()
        {
            var target = await this.GetTargetAndOpenAsync();

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            var serializedMessage = DataUtil.CreateRandomString(200);
            var serializedMessageBytes = Encoding.UTF8.GetBytes(serializedMessage);

            var cancellationToken = CancellationToken.None;

            _stream
                .Setup(s => s.CanWrite)
                .Returns(true)
                .Verifiable();

            _envelopeSerializer
                .Setup(e => e.Serialize(message))
                .Returns(serializedMessage);

            _stream
                .Setup(s =>
                    s.WriteAsync(
                        It.IsAny<byte[]>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<object>(null))
                .Verifiable();

            _traceWriter
                .Setup(t => t.IsEnabled)
                .Returns(true);

            await target.SendAsync(message, cancellationToken);

            _stream.Verify();

            _stream
                .Verify(s =>
                    s.WriteAsync(
                        It.Is<byte[]>(b => b.SequenceEqual(serializedMessageBytes)),
                        It.Is<int>(o => o == 0),
                        It.Is<int>(l => l == serializedMessageBytes.Length),
                        cancellationToken),
                    Times.Once());

            _traceWriter
                .Verify(t =>
                    t.TraceAsync(serializedMessage, DataOperation.Send),
                Times.Once());
        }

        [TestMethod]
        [TestCategory("SendAsync")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendAsync_NullEnvelope_ThrowsArgumentNullException()
        {
            var target = this.GetTarget();
            
            Envelope message = null;

            var cancellationToken = CancellationToken.None;

            await target.SendAsync(message, cancellationToken);
        }

        [TestMethod]
        [TestCategory("SendAsync")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task SendAsync_ClosedStream_ThrowsInvalidOperationException()
        {
            var target = this.GetTarget();

            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);

            var cancellationToken = CancellationToken.None;

            await target.SendAsync(message, cancellationToken);
        }

        #endregion

        #region ReceiveAsync

        [TestMethod]
        [TestCategory("ReceiveAsync")]
        public async Task ReceiveAsync_OneRead_ReadEnvelopeJsonFromStream()
        {
            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            var messageJson = DataUtil.CreateMessageJson();
            var cancelationToken = DataUtil.CreateCancellationToken();

            byte[] messageBuffer = Encoding.UTF8.GetBytes(
                messageJson);

            int bufferSize = messageBuffer.Length + DataUtil.CreateRandomInt(1000);
            var stream = new TestStream(messageBuffer);
            var target = await GetTargetAndOpenAsync(bufferSize, stream);

            _envelopeSerializer
                .Setup(e => e.Deserialize(messageJson))
                .Returns(message)
                .Verifiable();

            var actual = await target.ReceiveAsync(cancelationToken);

            _stream.Verify();
            _envelopeSerializer.Verify();

            Assert.AreEqual(message, actual);

            Assert.AreEqual(1, stream.ReadCount);
        }

        [TestMethod]
        [TestCategory("ReceiveAsync")]
        public async Task ReceiveAsync_MultipleReads_ReadEnvelopeJsonFromStream()
        {
            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            var messageJson = DataUtil.CreateMessageJson();
            var cancelationToken = DataUtil.CreateCancellationToken();

            var bufferParts = DataUtil.CreateRandomInt(10) + 1;

            byte[] messageBuffer = Encoding.UTF8.GetBytes(
                messageJson);

            var bufferPartSize = messageBuffer.Length / bufferParts;

            byte[][] messageBufferParts = new byte[bufferParts][];

            for (int i = 0; i < bufferParts; i++)
            {
                if (i + 1 == bufferParts)
                {
                    messageBufferParts[i] = messageBuffer
                        .Skip(i * bufferPartSize)
                        .ToArray();
                }
                else
                {
                    messageBufferParts[i] = messageBuffer
                    .Skip(i * bufferPartSize)
                    .Take(bufferPartSize)
                    .ToArray();
                }
            }

            int bufferSize = messageBuffer.Length + DataUtil.CreateRandomInt(1000);
            var stream = new TestStream(messageBufferParts);
            var target = await GetTargetAndOpenAsync(bufferSize, stream);

            _envelopeSerializer
                .Setup(e => e.Deserialize(messageJson))
                .Returns(message)
                .Verifiable();

            var actual = await target.ReceiveAsync(cancelationToken);

            _stream.Verify();
            _envelopeSerializer.Verify();

            Assert.AreEqual(message, actual);
            Assert.AreEqual(messageBufferParts.Length, stream.ReadCount);
        }

        [TestMethod]
        [TestCategory("ReceiveAsync")]
        public async Task ReceiveAsync_SingleReadBiggerThenBuffer_ClosesStreamAndThrowsInternalBufferOverflowException()
        {
            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            var messageJson = DataUtil.CreateMessageJson();
            var cancelationToken = DataUtil.CreateCancellationToken();

            byte[] messageBuffer = Encoding.UTF8.GetBytes(
                messageJson);

            int bufferSize = messageBuffer.Length - 1;
            var stream = new TestStream(messageBuffer);
            var target = await GetTargetAndOpenAsync(bufferSize, stream);

            _envelopeSerializer
                .Setup(e => e.Deserialize(messageJson))
                .Returns(message)
                .Verifiable();


            _envelopeSerializer
                .Setup(e => e.Deserialize(messageJson))
                .Returns(message)
                .Verifiable();

            try
            {
                var actual = await target.ReceiveAsync(cancelationToken);

                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is InternalBufferOverflowException);
                Assert.IsTrue(stream.CloseInvoked);
            }

        }

        [TestMethod]
        [TestCategory("ReceiveAsync")]
        public async Task ReceiveAsync_MultipleReadsBiggerThenBuffer_RaisesFailedAndThrowsInternalBufferOverflowException()
        {
            var content = DataUtil.CreateTextContent();
            var message = DataUtil.CreateMessage(content);
            var messageJson = DataUtil.CreateMessageJson();
            var cancelationToken = DataUtil.CreateCancellationToken();

            var bufferParts = DataUtil.CreateRandomInt(10) + 1;

            byte[] messageBuffer = Encoding.UTF8.GetBytes(
                messageJson);

            var bufferPartSize = messageBuffer.Length / bufferParts;

            byte[][] messageBufferParts = new byte[bufferParts][];

            for (int i = 0; i < bufferParts; i++)
            {
                if (i + 1 == bufferParts)
                {
                    messageBufferParts[i] = messageBuffer
                        .Skip(i * bufferPartSize)
                        .ToArray();
                }
                else
                {
                    messageBufferParts[i] = messageBuffer
                    .Skip(i * bufferPartSize)
                    .Take(bufferPartSize)
                    .ToArray();
                }
            }

            int bufferSize = messageBuffer.Length - 1;
            var stream = new TestStream(messageBufferParts);
            var target = await GetTargetAndOpenAsync(bufferSize, stream);


            _envelopeSerializer
                .Setup(e => e.Deserialize(messageJson))
                .Returns(message)
                .Verifiable();

            try
            {
                var actual = await target.ReceiveAsync(cancelationToken);

                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is InternalBufferOverflowException);
                Assert.IsTrue(stream.CloseInvoked);
            }
            
        }
        
        #endregion

        #region PerformCloseAsync

        [TestMethod]
        [TestCategory("PerformCloseAsync")]
        public async Task PerformCloseAsync_StreamOpened_ClosesStreamAndClient()
        {
            var cancellationToken = CancellationToken.None;

            _stream
                .Setup(s => s.Close())
                .Verifiable();

            _tcpClient
                .Setup(s => s.Close())
                .Verifiable();

            var target = await GetTargetAndOpenAsync();

            await target.CloseAsync(cancellationToken);

            _stream.Verify();
            _tcpClient.Verify();

        }

        [TestMethod]
        [TestCategory("PerformCloseAsync")]
        public async Task PerformCloseAsync_NoStream_ClosesClient()
        {
            var cancellationToken = CancellationToken.None;

            _tcpClient
                .Setup(s => s.Close())
                .Verifiable();

            var target = GetTarget();

            await target.CloseAsync(cancellationToken);

            _tcpClient.Verify();
        }

        #endregion

        #region GetSupportedEncryption

        [TestMethod]
        [TestCategory("GetSupportedEncryption")]
        public void GetSupportedEncryption_Default_ReturnsNoneAndTLS()
        {
            var target = GetTarget();

            var actual = target.GetSupportedEncryption();

            Assert.IsTrue(actual.Length == 2);
            Assert.IsTrue(actual.Contains(SessionEncryption.None));
            Assert.IsTrue(actual.Contains(SessionEncryption.TLS));

        }

        #endregion

        private class TestStream : Stream
        {

            private byte[][] _buffers;

            public TestStream(params byte[][] buffers)
            {
                _buffers = buffers;
            }

            public override bool CanRead 
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { throw new NotImplementedException(); }
            }

            public override bool CanWrite
            {
                get { throw new NotImplementedException(); }
            }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override long Length
            {
                get { throw new NotImplementedException(); }
            }

            public override long Position { get; set; }

            public int ReadCount { get; set; }


            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var currentBuffer = _buffers[ReadCount++ % _buffers.Length];

                Array.Copy(currentBuffer, 0, buffer, offset, currentBuffer.Length > count ? count : currentBuffer.Length);

                Position += currentBuffer.Length;

                return Task.FromResult(currentBuffer.Length);                
            }

            public bool CloseInvoked { get; set; }

            public override void Close()
            {
                CloseInvoked = true;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }
    }
}
