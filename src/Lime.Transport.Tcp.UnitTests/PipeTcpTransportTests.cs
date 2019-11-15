using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.UnitTests;
using Moq;
using Shouldly;
using System.Reflection;
using System.Runtime;
using Lime.Messaging;
using Lime.Protocol.Serialization.Newtonsoft;
using NUnit.Framework;

namespace Lime.Transport.Tcp.UnitTests
{
    [TestFixture]
    public class PipeTcpTransportTests 
    {
        [SetUp]
        public void SetUp()
        {
            EnvelopeSerializer = new EnvelopeSerializer(new DocumentTypeResolver().WithMessagingDocuments());
            ListenerUri = new Uri($"net.tcp://localhost:{Dummy.CreateRandomInt(50000) + 1000}");
            ClientIdentity = Dummy.CreateIdentity();
            ClientCertificate = CertificateUtil.CreateSelfSignedCertificate(ClientIdentity);
            ServerCertificate = CertificateUtil.GetOrCreateSelfSignedCertificate(ListenerUri.Host);
            CancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            MaxBufferSize = PipeTcpTransport.DEFAULT_PAUSE_WRITER_THRESHOLD;
        }
        
        [TearDown]
        public async Task TearDown()
        {
            await (TransportListener?.StopAsync(CancellationToken) ??  Task.CompletedTask);
            TransportListener?.DisposeIfDisposable();
            CancellationTokenSource?.Dispose();
        }
        
        public IEnvelopeSerializer EnvelopeSerializer { get; set; }
        
        public Identity ClientIdentity { get; set; }
        
        public X509Certificate2 ClientCertificate { get; set; }
        
        public X509Certificate2 ServerCertificate { get; set; }

        public Uri ListenerUri { get; set; }
        
        public PipeTcpTransportListener TransportListener { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public CancellationToken CancellationToken => CancellationTokenSource.Token;
        
        public int MaxBufferSize { get; set; }

        public ITraceWriter TraceWriter { get; set; }
        
        public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }
        
        public RemoteCertificateValidationCallback ClientCertificateValidationCallback { get; set; }
        
        private async Task<(PipeTcpTransport ClientTransport, PipeTcpTransport ServerTransport)> GetAndOpenTargets()
        {
            TransportListener = new PipeTcpTransportListener(ListenerUri, ServerCertificate, EnvelopeSerializer, MaxBufferSize, null, TraceWriter, ClientCertificateValidationCallback);
            await TransportListener.StartAsync(CancellationToken);
            var clientTransport = new PipeTcpTransport(EnvelopeSerializer, ClientCertificate, MaxBufferSize, TraceWriter, ServerCertificateValidationCallback);
            var serverTransportTask = TransportListener.AcceptTransportAsync(CancellationToken);
            await clientTransport.OpenAsync(ListenerUri, CancellationToken);
            var serverTransport = await serverTransportTask;
            await serverTransport.OpenAsync(ListenerUri, CancellationToken);
            return (clientTransport, (PipeTcpTransport)serverTransport);
        }

        [Test]
        [Category("OpenAsync")]
        public async Task OpenAsync_NotConnected_ShouldConnect()
        {
            // Act
            var (client, server) = await GetAndOpenTargets();
            
            // Assert
            client.IsConnected.ShouldBeTrue();
            server.IsConnected.ShouldBeTrue();
        }
        
        [Test]
        [Category("OpenAsync")]
        public async Task OpenAsync_AlreadyConnectedClient_ThrowsInvalidOperationException()
        {
            // Act
            var (client, _) = await GetAndOpenTargets();
            await client.OpenAsync(ListenerUri, CancellationToken).ShouldThrowAsync<InvalidOperationException>();
        }
        
        [Test]
        [Category("OpenAsync")]
        public async Task OpenAsync_AlreadyConnectedServer_ThrowsInvalidOperationException()
        {
            // Act
            var (_, server) = await GetAndOpenTargets();
            await server.OpenAsync(ListenerUri, CancellationToken).ShouldThrowAsync<InvalidOperationException>();
        }
        
        [Test]
        [Category("SendAsync")]
        public async Task SendAsync_SessionEnvelopeFromClient_ShouldBeReceivedByServer()
        {
            // Arrange
            var session = Dummy.CreateSession();
            var (client, server) = await GetAndOpenTargets();
            
            // Act
            await client.SendAsync(session, CancellationToken);
            
            // Assert
            var actual = await server.ReceiveAsync(CancellationToken);
            actual.ShouldNotBeNull();
            var actualSession = actual.ShouldBeOfType<Session>();
            actualSession.Id.ShouldBe(session.Id);
            actualSession.From.ShouldBe(session.From);
            actualSession.To.ShouldBe(session.To);
            actualSession.State.ShouldBe(session.State);
        }
        
        [Test]
        [Category("ReceiveAsync")]
        public async Task ReceiveAsync_SessionEnvelopeFromServer_ShouldBeReceivedByClient()
        {
            // Arrange
            var session = Dummy.CreateSession(SessionState.Established);
            var (client, server) = await GetAndOpenTargets();
            await server.SendAsync(session, CancellationToken);
            
            // Act
            var actual = await client.ReceiveAsync(CancellationToken);
            
            // Assert
            actual.ShouldNotBeNull();
            var actualSession = actual.ShouldBeOfType<Session>();
            actualSession.Id.ShouldBe(session.Id);
            actualSession.From.ShouldBe(session.From);
            actualSession.To.ShouldBe(session.To);
            actualSession.State.ShouldBe(session.State);
        }
    }
}
