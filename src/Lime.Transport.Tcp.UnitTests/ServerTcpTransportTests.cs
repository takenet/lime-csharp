using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Network.UnitTests;
using Lime.Protocol.UnitTests;
using Shouldly;
using System;
using System.Buffers;
using System.Threading.Tasks;
using Xunit;

namespace Lime.Transport.Tcp.UnitTests
{
    /// <summary>
    /// Tests for <see cref="TcpTransport"/> class using real TCP connections.
    /// </summary>
    public class ServerTcpTransportTests : ServerTransportTestsBase<TcpTransport, TcpTransport, TcpTransportListener>
    {
        public ServerTcpTransportTests()
            : base(new Uri("net.tcp://localhost:55321"))
        {
        }

        public int BufferSize { get; set; } = TcpTransport.DEFAULT_BUFFER_SIZE;

        public int MaxBufferSize { get; set; } = TcpTransport.DEFAULT_MAX_BUFFER_SIZE;

        public ArrayPool<byte> ArrayPool { get; set; } 

        protected override TcpTransport CreateClientTransport()
        {
            return new TcpTransport(EnvelopeSerializer);
        }

        protected override TcpTransportListener CreateTransportListener()
        {
            return new TcpTransportListener(ListenerUri, null, EnvelopeSerializer, BufferSize, MaxBufferSize, ArrayPool, traceWriter: TraceWriter.Object);
        }

        [Fact]
        public async Task ReceiveAsync_BiggerThanBufferSizeMessageEnvelope_ServerShouldReceive()
        {
            // Arrange
            BufferSize = 64;
            MaxBufferSize = BufferSize * 6;
            var message = Dummy.CreateMessage(Dummy.CreateRandomString(BufferSize * 4));
            var target = await GetTargetAsync();

            // Act
            await Client.SendAsync(message, CancellationToken);
            var actual = await target.ReceiveAsync(CancellationToken);

            // Assert
            actual.ShouldNotBeNull();
            var actualMessage = actual.ShouldBeOfType<Message>();
            CompareMessages(message, actualMessage);

        }

        [Fact]
        public async Task ReceiveAsync_MultipleBiggerThanBufferSizeMessageEnvelope_ServerShouldReceive()
        {
            // Arrange            
            BufferSize = 64;
            MaxBufferSize = BufferSize * 6;
            var count = Dummy.CreateRandomInt(100);
            var target = await GetTargetAsync();
            var messages = new Message[count];

            for (int i = 0; i < count; i++)
            {
                messages[i] = Dummy.CreateMessage(Dummy.CreateRandomString(BufferSize * (3 + i % 2)));
            }
            var actuals = new Envelope[count];


            // Act
            for (int i = 0; i < count; i++)
            {
                await Client.SendAsync(messages[i], CancellationToken);
                actuals[i] = await target.ReceiveAsync(CancellationToken);
            }

            // Assert
            for (int i = 0; i < count; i++)
            {
                var actual = actuals[i];
                var message = messages[i];
                actual.ShouldNotBeNull();
                var actualMessage = actual.ShouldBeOfType<Message>();
                CompareMessages(message, actualMessage);
            }
        }

        [Fact]
        public async Task ReceiveAsync_BiggerThanMaxBufferSizeMessageEnvelope_ServerShouldThrowBufferOverflowException()
        {
            // Arrange
            BufferSize = 64;
            MaxBufferSize = BufferSize * 4;
            var message = Dummy.CreateMessage(Dummy.CreateRandomString(BufferSize * 5));
            var target = await GetTargetAsync();

            // Act
            await Client.SendAsync(message, CancellationToken);
            try
            {
                var actual = await target.ReceiveAsync(CancellationToken);
                throw new Exception("The expected exception didn't occur");
            }
            catch (BufferOverflowException)
            {
                target.IsConnected.ShouldBe(false);
            }
        }

        private static void CompareMessages(Message message, Message actualMessage)
        {
            actualMessage.Id.ShouldBe(message.Id);
            actualMessage.From.ShouldBe(message.From);
            actualMessage.To.ShouldBe(message.To);
            actualMessage.Pp.ShouldBe(message.Pp);
            actualMessage.Metadata.ShouldBe(message.Metadata);
            actualMessage.Type.ShouldBe(message.Type);
            actualMessage.Content.ToString().ShouldBe(message.Content.ToString());
            actualMessage.Metadata.ShouldBe(message.Metadata);
        }
    }
}
