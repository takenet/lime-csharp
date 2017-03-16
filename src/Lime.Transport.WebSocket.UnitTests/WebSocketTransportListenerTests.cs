using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.UnitTests;
using Moq;
using NUnit.Framework;
using Shouldly;
using Xunit;

namespace Lime.Transport.WebSocket.UnitTests
{

    public class WebSocketTransportListenerTests : IDisposable
    {
        public WebSocketTransportListenerTests()
        {
            ListenerUri = new Uri("ws://localhost:8081");
            EnvelopeSerializer = new JsonNetSerializer();
            TraceWriter = new Mock<ITraceWriter>();
            Target = new WebSocketTransportListener(ListenerUri, SslCertificate, EnvelopeSerializer, TraceWriter.Object);

            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
        }

        public WebSocketTransportListener Target { get; private set; }

        public Uri ListenerUri { get; private set; }

        public X509CertificateInfo SslCertificate { get; private set; }

        public IEnvelopeSerializer EnvelopeSerializer { get; private set; }

        public Mock<ITraceWriter> TraceWriter { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

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
            await Target.StartAsync(CancellationToken);

            // Assert
            var acceptTransportTask = Target.AcceptTransportAsync(CancellationToken);
            var clientTransport = new ClientWebSocketTransport(EnvelopeSerializer);
            await clientTransport.OpenAsync(ListenerUri, CancellationToken);
        }

        [Fact]
        public async Task AcceptTransportAsync_NewConnection_RetunsTransport()
        {
            // Arrange
            await Target.StartAsync(CancellationToken);
            var clientTransport = new ClientWebSocketTransport(EnvelopeSerializer);


            // Act
            var acceptTransportTask = Target.AcceptTransportAsync(CancellationToken);
            await clientTransport.OpenAsync(ListenerUri, CancellationToken);
            var transport = await acceptTransportTask;

            // Assert
            transport.ShouldNotBeNull();
        }

        [Fact]
        public async Task AcceptTransportAsync_MultipleConnections_RetunsTransports()
        {
            // Arrange
            await Target.StartAsync(CancellationToken);

            var count = Dummy.CreateRandomInt(100) + 1;
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
            await Target.StartAsync(CancellationToken);

            // Act
            await Target.StopAsync(CancellationToken);

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

        public void Dispose()
        {
            try
            {
                Target.StopAsync(CancellationToken).Wait();
            }
            catch (AggregateException) { }
        }
    }
}