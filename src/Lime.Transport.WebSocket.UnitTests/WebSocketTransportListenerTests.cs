using System;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Transport.WebSocket.UnitTests
{
    [TestFixture]
    public class WebSocketTransportListenerTests
    {

        public WebSocketTransportListener Target { get; private set; }

        public Uri ListenerUri { get; private set; }

        public X509Certificate2 SslCertificate { get; private set; }

        public IEnvelopeSerializer EnvelopeSerializer { get; private set; }

        public Mock<ITraceWriter> TraceWriter { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        [SetUp]
        public void SetUp()
        {
            ListenerUri = new Uri("ws://localhost:8081");
            EnvelopeSerializer = new EnvelopeSerializer();
            TraceWriter = new Mock<ITraceWriter>();
            Target = new WebSocketTransportListener(ListenerUri, SslCertificate, EnvelopeSerializer, TraceWriter.Object);

            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Target.StopAsync().Wait();
            }
            catch (AggregateException) { }
        }

        [Test]
        public void ListenerUris_ValidHostAndPort_GetsRegisteredUris()
        {
            // Act
            var listenerUris = Target.ListenerUris;

            // Assert
            listenerUris.ShouldNotBe(null);
            listenerUris.Length.ShouldBe(1);
            listenerUris[0].ShouldBe(ListenerUri);
        }

        [Test]
        public async Task StartAsync_ValidHostAndPort_ServerStarted()
        {
            // Act
            await Target.StartAsync();

            // Assert
            var clientTransport = new ClientWebSocketTransport(EnvelopeSerializer);
            await clientTransport.OpenAsync(ListenerUri, CancellationToken);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task StartAsync_CallTwice_ThrowsInvalidOperationException()
        {
            // Act
            await Target.StartAsync();
            await Target.StartAsync();
        }

        [Test]
        public async Task AcceptTransportAsync_NewRequest_RetunsTransport()
        {
            // Arrange
            await Target.StartAsync();
            var clientTransport = new ClientWebSocketTransport(EnvelopeSerializer);
            await clientTransport.OpenAsync(ListenerUri, CancellationToken);

            // Act
            var transport = await Target.AcceptTransportAsync(CancellationToken);

            // Assert
            transport.ShouldNotBeNull();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task AcceptTransportAsync_ListenerNotStarted_ThrowsInvalidOperationException()
        {
            // Act
            var transport = await Target.AcceptTransportAsync(CancellationToken);
        }

        [Test]
        public async Task StopAsync_ActiveListener_StopsListening()
        {
            // Arrange
            await Target.StartAsync();

            // Act
            await Target.StopAsync();

            // Assert
            try
            {
                var clientTransport = new ClientWebSocketTransport(EnvelopeSerializer);
                await clientTransport.OpenAsync(ListenerUri, CancellationToken);
                Assert.Fail("The listener is active");
            }
            catch (WebSocketException ex)
            {
                ex.NativeErrorCode.ShouldBe(10061);
            }
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task StopAsync_ListenerNotStarted_ThrowsInvalidOperationException()
        {
            // Act
            await Target.StopAsync();
        }

    }
}