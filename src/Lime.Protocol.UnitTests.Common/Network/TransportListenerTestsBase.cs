using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Moq;
using NUnit.Framework;
using Shouldly;
using Lime.Protocol.Server;
using System.Net.Sockets;

namespace Lime.Protocol.UnitTests.Network
{
    public abstract class TransportListenerTestsBase<TClientTransport, TTransportListener>
        where TClientTransport : class, ITransport
        where TTransportListener : class, ITransportListener
    {
        protected void SetUp(Uri listenerUri)
        {
            ListenerUri = listenerUri;
            EnvelopeSerializer = new JsonNetSerializer();
            TraceWriter = new Mock<ITraceWriter>();           
            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
            Target = CreateTransportListener();
        }

        [TearDown]
        public async Task TearDown()
        {
            try
            {
                await Target.StopAsync(CancellationToken);
            }
            catch (InvalidOperationException) { }
            Target.DisposeIfDisposable();
            Target = null;
        }

        protected abstract TTransportListener CreateTransportListener();

        protected abstract TClientTransport CreateClientTransport();

        public TTransportListener Target { get; private set; }

        public Uri ListenerUri { get; private set; }        

        public IEnvelopeSerializer EnvelopeSerializer { get; private set; }

        public Mock<ITraceWriter> TraceWriter { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

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
            await Target.StartAsync(CancellationToken);

            // Assert
            var acceptTransportTask = Target.AcceptTransportAsync(CancellationToken);
            var clientTransport = CreateClientTransport();
            await clientTransport.OpenAsync(ListenerUri, CancellationToken);
        }

        [Test]
        public async Task AcceptTransportAsync_NewConnection_RetunsTransport()
        {
            // Arrange
            await Target.StartAsync(CancellationToken);
            var clientTransport = CreateClientTransport();


            // Act
            var acceptTransportTask = Target.AcceptTransportAsync(CancellationToken);
            await clientTransport.OpenAsync(ListenerUri, CancellationToken);
            var transport = await acceptTransportTask;

            // Assert
            transport.ShouldNotBeNull();
        }

        [Test]
        public async Task AcceptTransportAsync_MultipleConnections_RetunsTransports()
        {
            // Arrange
            await Target.StartAsync(CancellationToken);

            var count = Dummy.CreateRandomInt(100) + 1;
            var clientTransports = Enumerable.Range(0, count)
                .Select(async i =>
                {
                    var clientTransport = CreateClientTransport();
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

        [Test]
        public async Task AcceptTransportAsync_ListenerNotStarted_ThrowsInvalidOperationException()
        {
            // Act
            var transport = await Target.AcceptTransportAsync(CancellationToken).ShouldThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task StopAsync_ActiveListener_StopsListening()
        {
            // Arrange
            await Target.StartAsync(CancellationToken);

            // Act
            await Target.StopAsync(CancellationToken);

            // Assert
            try
            {
                var clientTransport = CreateClientTransport();
                await clientTransport.OpenAsync(ListenerUri, CancellationToken);
                throw new Exception("The listener is active");
            }
            catch (WebSocketException ex)
            {
                ex.NativeErrorCode.ShouldBe(10061);
            }
            catch (SocketException ex)
            {
                ex.ErrorCode.ShouldBe(10061);
            }
        }
    }
}