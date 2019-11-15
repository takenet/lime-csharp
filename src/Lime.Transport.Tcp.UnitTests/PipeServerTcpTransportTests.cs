using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.UnitTests;
using Shouldly;
using System;
using System.Buffers;
using System.Reflection;
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
    public class PipeServerTcpTransportTests : ServerTransportTestsBase
    {
        public int BufferSize { get; set; }

        public int MaxBufferSize { get; set; }

        public MemoryPool<byte> MemoryPool { get; } = MemoryPool<byte>.Shared;
        
        protected override Task SetUpImpl()
        {
            BufferSize = TcpTransport.DEFAULT_BUFFER_SIZE;
            MaxBufferSize = TcpTransport.DEFAULT_MAX_BUFFER_SIZE;
            
            return base.SetUpImpl();
        }
     
        protected override Uri CreateListenerUri() => new Uri("net.tcp://localhost:55323");

        protected override ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer)
        {
            return new PipeTcpTransportListener(uri, null, envelopeSerializer, BufferSize, MaxBufferSize, MemoryPool, TraceWriter.Object);
        }

        protected override ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer)
        {
            return new PipeTcpTransport(envelopeSerializer);
        }        

        [Test]
        public async Task ReceiveAsync_BiggerThanBufferSizeMessageEnvelope_ServerShouldReceive()
        {
            // Arrange
            BufferSize = 64;
            MaxBufferSize = BufferSize * 6;
            var message = Dummy.CreateMessage(Dummy.CreateRandomString(BufferSize * 4));
            var target = await GetTargetAsync();

            // Act
            await ClientTransport.SendAsync(message, CancellationToken);
            var actual = await target.ReceiveAsync(CancellationToken);

            // Assert
            actual.ShouldNotBeNull();
            var actualMessage = actual.ShouldBeOfType<Message>();
            CompareMessages(message, actualMessage);
//            var jsonBuffer = GetJsonBuffer(target);
//            jsonBuffer.Buffer.Length.ShouldBe(BufferSize);

        }

        [Test]
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
                await ClientTransport.SendAsync(messages[i], CancellationToken);
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

//            var jsonBuffer = GetJsonBuffer(target);
//            jsonBuffer.Buffer.Length.ShouldBe(BufferSize);
        }


        [Test]
        public async Task ReceiveAsync_DoubleWritesAndReads_ServerShouldReceive()
        {
            // Arrange            
            BufferSize = 64;
            MaxBufferSize = BufferSize * 12;
            var count = 50;
            var target = await GetTargetAsync();
            var messages = new Message[count];

            for (int i = 0; i < count; i++)
            {
                messages[i] = Dummy.CreateMessage(Dummy.CreateRandomString(BufferSize * 3));
            }
            var actuals = new Envelope[count];


            // Act
            for (int i = 0; i < count; i += 2)
            {
                await ClientTransport.SendAsync(messages[i], CancellationToken);
                await ClientTransport.SendAsync(messages[i + 1], CancellationToken);
                actuals[i] = await target.ReceiveAsync(CancellationToken);
                actuals[i + 1] = await target.ReceiveAsync(CancellationToken);
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
//
//            var jsonBuffer = GetJsonBuffer(target);
//            jsonBuffer.Buffer.Length.ShouldBe(BufferSize);
        }

        [Test]
        public async Task ReceiveAsync_InterleavedWritesAndReads_ServerShouldReceive()
        {
            // Arrange            
            BufferSize = 64;
            MaxBufferSize = BufferSize * 24;
            var pageSize = 5;
            var count = 9 * pageSize;
            var target = await GetTargetAsync();
            var messages = new Message[count];

            for (int i = 0; i < count; i++)
            {
                messages[i] = Dummy.CreateMessage(Dummy.CreateRandomString(BufferSize * 3));
            }
            var actuals = new Envelope[count];


            // Act
            for (int i = 0; i < count; i += pageSize)
            {
                await ClientTransport.SendAsync(messages[i], CancellationToken);
                await ClientTransport.SendAsync(messages[i + 1], CancellationToken);
                await ClientTransport.SendAsync(messages[i + 2], CancellationToken);
                actuals[i] = await target.ReceiveAsync(CancellationToken);
                actuals[i + 1] = await target.ReceiveAsync(CancellationToken);
                await ClientTransport.SendAsync(messages[i + 3], CancellationToken);
                actuals[i + 2] = await target.ReceiveAsync(CancellationToken);
                await ClientTransport.SendAsync(messages[i + 4], CancellationToken);
                actuals[i + 3] = await target.ReceiveAsync(CancellationToken);
                actuals[i + 4] = await target.ReceiveAsync(CancellationToken);
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
//
//            var jsonBuffer = GetJsonBuffer(target);
//            jsonBuffer.Buffer.Length.ShouldBe(BufferSize);
        }

        [Test]
        public async Task ReceiveAsync_BiggerThanMaxBufferSizeMessageEnvelope_ServerShouldThrowBufferOverflowException()
        {
            // Arrange
            BufferSize = 64;
            MaxBufferSize = BufferSize * 4;
            var message = Dummy.CreateMessage(Dummy.CreateRandomString(BufferSize * 5));
            var target = await GetTargetAsync();

            // Act
            await ClientTransport.SendAsync(message, CancellationToken);
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
