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
    public class TcpTransportTests : TransportTestsBase
    {
        public int BufferSize { get; set; }

        public int ClientMaxBufferSize { get; set; }

        public int ServerMaxBufferSize { get; set; }

        public ArrayPool<byte> ArrayPool { get; } = ArrayPool<byte>.Shared;
        
        protected override Task SetUpImpl()
        {
            BufferSize = TcpTransport.DEFAULT_BUFFER_SIZE;
            ClientMaxBufferSize = TcpTransport.DEFAULT_MAX_BUFFER_SIZE;
            ServerMaxBufferSize = TcpTransport.DEFAULT_MAX_BUFFER_SIZE;

            return base.SetUpImpl();
        }
        
        protected override Uri CreateListenerUri() => new Uri("net.tcp://localhost:5321");

        protected override ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer) 
            => new TcpTransportListener(uri, null, envelopeSerializer, BufferSize, ServerMaxBufferSize, ArrayPool, TraceWriter);

        protected override ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer) => new TcpTransport(envelopeSerializer, null, BufferSize, ClientMaxBufferSize);

        [Test]
        public async Task ReceiveAsync_BiggerThanBufferSizeMessageEnvelope_ServerShouldReceive()
        {
            // Arrange
            BufferSize = 64;
            ServerMaxBufferSize = ClientMaxBufferSize = BufferSize * 10;
            var message = Dummy.CreateMessage(Dummy.CreateRandomString(BufferSize * 4));
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();

            // Act
            await clientTransport.SendAsync(message, CancellationToken);
            var actual = await serverTransport.ReceiveAsync(CancellationToken);

            // Assert
            actual.ShouldNotBeNull();
            var actualMessage = actual.ShouldBeOfType<Message>();
            CompareMessages(message, actualMessage);
            var jsonBuffer = GetJsonBuffer(serverTransport);
            jsonBuffer.Buffer.Length.ShouldBe(BufferSize);
        }

        [Test]
        public async Task ReceiveAsync_MultipleBiggerThanBufferSizeMessageEnvelope_ServerShouldReceive()
        {
            // Arrange            
            BufferSize = 64;
            ServerMaxBufferSize = ClientMaxBufferSize = BufferSize * 10;
            var count = Dummy.CreateRandomInt(100);
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();
            var synchronizedserverTransport = new SynchronizedTransportDecorator(serverTransport);
            var synchronizedClient = new SynchronizedTransportDecorator(clientTransport);
            
            var messages = new Message[count];

            for (int i = 0; i < count; i++)
            {
                messages[i] = Dummy.CreateMessage(Dummy.CreateRandomString(BufferSize * (3 + i % 2)));
            }
            var actuals = new Envelope[count];


            // Act
            for (int i = 0; i < count; i++)
            {
                await synchronizedClient.SendAsync(messages[i], CancellationToken);
                actuals[i] = await synchronizedserverTransport.ReceiveAsync(CancellationToken);
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

            var jsonBuffer = GetJsonBuffer(serverTransport);
            jsonBuffer.Buffer.Length.ShouldBe(BufferSize);
        }


        [Test]
        public async Task ReceiveAsync_DoubleWritesAndReads_ServerShouldReceive()
        {
            // Arrange            
            BufferSize = 64;
            ServerMaxBufferSize = ClientMaxBufferSize = BufferSize * 12;
            var count = 50;
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();
            var messages = new Message[count];

            for (int i = 0; i < count; i++)
            {
                messages[i] = Dummy.CreateMessage(Dummy.CreateRandomString(BufferSize * 3));
            }
            var actuals = new Envelope[count];


            // Act
            for (int i = 0; i < count; i += 2)
            {
                await clientTransport.SendAsync(messages[i], CancellationToken);
                await clientTransport.SendAsync(messages[i + 1], CancellationToken);
                actuals[i] = await serverTransport.ReceiveAsync(CancellationToken);
                actuals[i + 1] = await serverTransport.ReceiveAsync(CancellationToken);
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

            var jsonBuffer = GetJsonBuffer(serverTransport);
            jsonBuffer.Buffer.Length.ShouldBe(BufferSize);
        }

        [Test]
        public async Task ReceiveAsync_InterleavedWritesAndReads_ServerShouldReceive()
        {
            // Arrange            
            BufferSize = 64;
            ServerMaxBufferSize = ClientMaxBufferSize = BufferSize * 24;
            var pageSize = 5;
            var count = 9 * pageSize;
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();
            var messages = new Message[count];

            for (int i = 0; i < count; i++)
            {
                messages[i] = Dummy.CreateMessage(Dummy.CreateRandomString(BufferSize * 3));
            }
            var actuals = new Envelope[count];


            // Act
            for (int i = 0; i < count; i += pageSize)
            {
                await clientTransport.SendAsync(messages[i], CancellationToken);
                await clientTransport.SendAsync(messages[i + 1], CancellationToken);
                await clientTransport.SendAsync(messages[i + 2], CancellationToken);
                actuals[i] = await serverTransport.ReceiveAsync(CancellationToken);
                actuals[i + 1] = await serverTransport.ReceiveAsync(CancellationToken);
                await clientTransport.SendAsync(messages[i + 3], CancellationToken);
                actuals[i + 2] = await serverTransport.ReceiveAsync(CancellationToken);
                await clientTransport.SendAsync(messages[i + 4], CancellationToken);
                actuals[i + 3] = await serverTransport.ReceiveAsync(CancellationToken);
                actuals[i + 4] = await serverTransport.ReceiveAsync(CancellationToken);
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

            var jsonBuffer = GetJsonBuffer(serverTransport);
            jsonBuffer.Buffer.Length.ShouldBe(BufferSize);
        }

        [Test]
        public async Task ReceiveAsync_BiggerThanMaxBufferSizeMessageEnvelope_ServerShouldThrowBufferOverflowException()
        {
            // Arrange
            BufferSize = 64;
            ClientMaxBufferSize = BufferSize * 8;
            ServerMaxBufferSize = BufferSize * 4;
            var message = Dummy.CreateMessage(Dummy.CreateRandomString(BufferSize * 4));
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();

            // Act
            await clientTransport.SendAsync(message, CancellationToken);
            try
            {
                var actual = await serverTransport.ReceiveAsync(CancellationToken);
                throw new Exception("The expected exception didn't occur");
            }
            catch (BufferOverflowException)
            {
                serverTransport.IsConnected.ShouldBe(false);
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

        private static JsonBuffer GetJsonBuffer(ITransport serverTransport)
        {
            var transport = serverTransport;
            if (transport is SynchronizedTransportDecorator)
            {
                transport = (ITransport)typeof(SynchronizedTransportDecorator)
                    .GetField("_transport", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(transport);
            }
            
            return (JsonBuffer)transport.GetType().GetField("_jsonBuffer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(transport);
        }
    }
}
