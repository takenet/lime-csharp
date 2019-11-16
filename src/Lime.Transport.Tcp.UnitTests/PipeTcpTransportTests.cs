using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.UnitTests;
using Shouldly;
using System;
using System.Buffers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests.Common.Network;
using NUnit.Framework;

namespace Lime.Transport.Tcp.UnitTests
{
    /// <summary>
    /// Tests for <see cref="TcpTransport"/> class using real TCP connections.
    /// </summary>    
    [TestFixture]    
    public class PipeTcpTransportTests : TransportTestsBase
    {
        public int PauseWriterThreshold { get; set; }

        public MemoryPool<byte> MemoryPool { get; } = MemoryPool<byte>.Shared;
        
        protected override Task SetUpImpl()
        {
            PauseWriterThreshold = PipeTcpTransport.DEFAULT_PAUSE_WRITER_THRESHOLD;
            
            return base.SetUpImpl();
        }
     
        protected override Uri CreateListenerUri() => new Uri("net.tcp://localhost:55323");

        protected override ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer)
        {
            return new PipeTcpTransportListener(uri, null, envelopeSerializer, PauseWriterThreshold, MemoryPool, TraceWriter.Object);
        }

        protected override ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer)
        {
            return new PipeTcpTransport(envelopeSerializer, pauseWriterThreshold: PauseWriterThreshold);
        }

        [Test]
        public async Task SendAsync_BiggerThanPauseWriterThresholdMessageEnvelope_TransportShouldBeClosed()
        {
            // Arrange
            PauseWriterThreshold = 1024;
            var message = Dummy.CreateMessage(Dummy.CreateRandomString(PauseWriterThreshold * 3));
            var (_, serverTransport) = await GetAndOpenTargetsAsync();

            // Act
            try
            {
                await serverTransport.SendAsync(message, CancellationToken);
            }
            catch (Exception ex)
            {
                var argumentException = ex.ShouldBeOfType<ArgumentException>();
                argumentException.ParamName.ShouldBe("envelope");
                serverTransport.IsConnected.ShouldBeFalse();
            }
        }
    }
}
