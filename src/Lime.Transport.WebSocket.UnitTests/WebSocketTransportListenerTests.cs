using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.UnitTests;
using Moq;
using Xunit;
using Shouldly;

namespace Lime.Transport.WebSocket.UnitTests
{
    
    public class WebSocketTransportListenerTests : IDisposable
    {

        public WebSocketTransportListener Target { get; private set; }

        public Uri ListenerUri { get; private set; }

        public X509Certificate2 SslCertificate { get; private set; }

        public IEnvelopeSerializer EnvelopeSerializer { get; private set; }

        public Mock<ITraceWriter> TraceWriter { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public WebSocketTransportListenerTests()
        {
            ListenerUri = new Uri("ws://localhost:8081");
            EnvelopeSerializer = new JsonNetSerializer();
            TraceWriter = new Mock<ITraceWriter>();
            Target = new WebSocketTransportListener(ListenerUri, SslCertificate, EnvelopeSerializer, TraceWriter.Object);

            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
        }

        public void Dispose()
        {
            try
            {
                Target.StopAsync().Wait(CancellationToken);
            }
            catch (AggregateException) { }
        }

        [Fact]
        public void ListenerUris_ValidHostAndPort_GetsRegisteredUris()
        {
            // Act
            var listenerUris = Target.ListenerUris;

            // Assert
            listenerUris.ShouldNotBe(null);
            listenerUris.Length.ShouldBe(1);
            listenerUris[0].ShouldBe(ListenerUri);
        }

        [Fact]
        public async Task StartAsync_ValidHostAndPort_ServerStarted()
        {
            // Act
            await Target.StartAsync();

            // Assert
            var clientTransport = new ClientWebSocketTransport(EnvelopeSerializer);
            await clientTransport.OpenAsync(ListenerUri, CancellationToken);
        }

        [Fact]
        public async Task StartAsync_CallTwice_ThrowsInvalidOperationException()
        {
            // Act
            await Target.StartAsync();
            await Target.StartAsync().ShouldThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task AcceptTransportAsync_NewConnection_RetunsTransport()
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

        [Fact]
        public async Task AcceptTransportAsync_MultipleConnections_RetunsTransports()
        {
            // Arrange
            await Target.StartAsync();

            var count = Dummy.CreateRandomInt(10) + 1;
            var clientTransports = Enumerable.Range(0, count)
                .Select(async i =>
                {
                    var clientTransport = new ClientWebSocketTransport(EnvelopeSerializer);
                    await clientTransport.OpenAsync(ListenerUri, CancellationToken);
                    return clientTransport;
                })
                .ToList();
            
            // Act
            var acceptTasks = new List<Task<ITransport>>();
            while (count-- > 0)
            {
                acceptTasks.Add(
                    Task.Run(async () => await Target.AcceptTransportAsync(CancellationToken), CancellationToken));
            }

            await Task.WhenAll(acceptTasks);
            var actualTransports = acceptTasks.Select(t => t.Result).ToList();

            // Assert
            actualTransports.Count.ShouldBe(clientTransports.Count);
        }

        [Fact]
        public async Task AcceptTransportAsync_ListenerNotStarted_ThrowsInvalidOperationException()
        {
            // Act
            var transport = await Target.AcceptTransportAsync(CancellationToken).ShouldThrowAsync<InvalidOperationException>();
        }

        [Fact]
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
                throw new Exception("The listener is active");
            }
            catch (WebSocketException ex)
            {
                ex.NativeErrorCode.ShouldBe(10061);
            }
        }

        [Fact]
        public async Task StopAsync_ListenerNotStarted_ThrowsInvalidOperationException()
        {
            // Act
            await Target.StopAsync().ShouldThrowAsync<InvalidOperationException>();
        }

    }
}