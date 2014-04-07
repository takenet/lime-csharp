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
        private async Task<TcpTransport> GetTargetAndOpenAsync()
        {
            var uri = DataUtil.CreateUri(Uri.UriSchemeNetTcp);
            var cancellationToken = CancellationToken.None;

            var readTcs = new TaskCompletionSource<int>();

            _tcpClient
                .Setup(c => c.Connected)
                .Returns(true)
                .Verifiable();

            _stream
                .Setup(s => s.CanRead)
                .Returns(true)
                .Verifiable();

            _stream
                .Setup(
                    s => s.ReadAsync(
                        It.IsAny<byte[]>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<CancellationToken>()))
                .Returns(readTcs.Task)
                .Verifiable();

            var target = GetTarget();

            await target.OpenAsync(uri, cancellationToken);
            return target;
        }  

        #endregion

        #region OpenAsync

        [TestMethod]
        [TestCategory("OpenAsync")]
        public async Task OpenAsync_NotConnectedValidUri_ConnectsClientAndCallsStreamReadAsync()
        {
            var uri = DataUtil.CreateUri(Uri.UriSchemeNetTcp);
            var cancellationToken = CancellationToken.None;
            int offset = 0;
            int bufferSize = DataUtil.CreateRandomInt(10000);

            var readTcs = new TaskCompletionSource<int>();

            _tcpClient
                .Setup(c => c.Connected)
                .Returns(false)
                .Verifiable();

            _stream
                .Setup(s => s.CanRead)
                .Returns(true)
                .Verifiable();
           
            _stream
                .Setup(
                    s => s.ReadAsync(
                        It.Is<byte[]>(b => b.Length == bufferSize), 
                        offset, 
                        bufferSize, 
                        It.IsAny<CancellationToken>()))
                .Returns(readTcs.Task)
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
        public async Task OpenAsync_AlreadyConnectedValidUri_CallsStreamReadAsync()
        {
            var uri = DataUtil.CreateUri(Uri.UriSchemeNetTcp);
            var cancellationToken = CancellationToken.None;
            int offset = 0;
            int bufferSize = DataUtil.CreateRandomInt(10000);

            var readTcs = new TaskCompletionSource<int>();

            _tcpClient
                .Setup(c => c.Connected)
                .Returns(true)
                .Verifiable();

            _stream
                .Setup(s => s.CanRead)
                .Returns(true)
                .Verifiable();

            _stream
                .Setup(
                    s => s.ReadAsync(
                        It.Is<byte[]>(b => b.Length == bufferSize),
                        offset,
                        bufferSize,
                        It.IsAny<CancellationToken>()))
                .Returns(readTcs.Task)
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
        }

        [TestMethod]
        [TestCategory("OpenAsync")]
        public async Task OpenAsync_ReadAsyncThrowsException_RaisesFailed()
        {
            var uri = DataUtil.CreateUri(Uri.UriSchemeNetTcp);
            var cancellationToken = CancellationToken.None;
            int offset = 0;
            int bufferSize = DataUtil.CreateRandomInt(10000);
            var readTcs = new TaskCompletionSource<int>();
            var exception = DataUtil.CreateException();
            readTcs.SetException(exception);

            bool failedRaised = false;

            _tcpClient
                .Setup(c => c.Connected)
                .Returns(true)
                .Verifiable();

            _stream
                .Setup(s => s.CanRead)
                .Returns(true)
                .Verifiable();

            _stream
                .Setup(
                    s => s.ReadAsync(
                        It.Is<byte[]>(b => b.Length == bufferSize),
                        offset,
                        bufferSize,
                        It.IsAny<CancellationToken>()))
                .Returns(readTcs.Task);

            var setFailedTcs = new TaskCompletionSource<object>();
            var timeoutCancellationTokenSource = new CancellationTokenSource(500);
            timeoutCancellationTokenSource.Token.Register(() => setFailedTcs.TrySetCanceled());
                       
            var target = GetTarget(bufferSize: bufferSize);
            target.Failed += (sender, e) =>
            {
                using (e.GetDeferral())
                {
                    failedRaised = !failedRaised && e.Exception == exception;
                }

                setFailedTcs.TrySetResult(null);
            };


            await target.OpenAsync(uri, cancellationToken);
            await setFailedTcs.Task;

            Assert.IsTrue(failedRaised);
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
    }
}
