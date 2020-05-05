using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.UnitTests;
using Shouldly;
using System;
using System.Buffers;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Client;
using Lime.Protocol.Security;
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

        public X509Certificate2 ServerCertificate { get; set; }
        
        public X509Certificate2 ClientCertificate { get; set; }

        public RemoteCertificateValidationCallback ClientCertificateValidationCallback { get; set; }

        public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }
        
        protected override Task SetUpImpl()
        {
            PauseWriterThreshold = EnvelopePipe.DEFAULT_PAUSE_WRITER_THRESHOLD;
            ServerCertificate = null;
            ClientCertificate = null;
            ClientCertificateValidationCallback = null;
            ServerCertificateValidationCallback = null; 
            
            return base.SetUpImpl();
        }
     
        protected override Uri CreateListenerUri() => new Uri("net.tcp://localhost:5331");

        protected override ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer)
        {
            return new PipeTcpTransportListener(uri, ServerCertificate, envelopeSerializer, PauseWriterThreshold, MemoryPool, TraceWriter, ClientCertificateValidationCallback);
        }

        protected override ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer)
        {
            return new PipeTcpTransport(envelopeSerializer, pauseWriterThreshold: PauseWriterThreshold, clientCertificate: ClientCertificate, serverCertificateValidationCallback: ServerCertificateValidationCallback);
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
                ex.ShouldBeOfType<InvalidOperationException>();
                serverTransport.IsConnected.ShouldBeFalse();
            }
        }
        
        [Test]
        public async Task GetSupportedEncryptionOptions_ServerWithoutServerCertificateDefined_ShouldReturnNone()
        {
            // Arrange
            ServerCertificate = null;
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();
            
            // Act
            var serverSupportedEncryption = serverTransport.GetSupportedEncryption();
            
            // Assert
            serverSupportedEncryption.ShouldContain(SessionEncryption.None);
            serverSupportedEncryption.ShouldNotContain(SessionEncryption.TLS);
        }
        
        [Test]
        public async Task GetSupportedEncryptionOptions_ServerWithServerCertificateDefined_ShouldReturnNoneTls()
        {
            // Arrange
            ServerCertificate = CertificateUtil.GetOrCreateSelfSignedCertificate("localhost");
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();
            
            // Act
            var serverSupportedEncryption = serverTransport.GetSupportedEncryption();
            
            // Assert
            serverSupportedEncryption.ShouldContain(SessionEncryption.None);
            serverSupportedEncryption.ShouldContain(SessionEncryption.TLS);
        }
        
        [Test]
        public async Task GetSupportedEncryptionOptions_Client_ShouldReturnNoneTls()
        {
            // Arrange
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();
            
            // Act
            var serverSupportedEncryption = clientTransport.GetSupportedEncryption();
            
            // Assert
            serverSupportedEncryption.ShouldContain(SessionEncryption.None);
            serverSupportedEncryption.ShouldContain(SessionEncryption.TLS);
        }

        [Test]
        public async Task SetEncryptionAsync_UpgradeToTls_ShouldSucceed()
        {
            // Arrange
            ServerCertificate = CertificateUtil.GetOrCreateSelfSignedCertificate("localhost");
            ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            var newSession = Dummy.CreateSession();
            var authenticatingSession = Dummy.CreateSession(SessionState.Authenticating);
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();
            
            // Act
            await clientTransport.SendAsync(newSession, CancellationToken);
            var receivedEnvelopeBeforeUpgrade = await serverTransport.ReceiveAsync(CancellationToken);
            await Task.WhenAll(
                serverTransport.SetEncryptionAsync(SessionEncryption.TLS, CancellationToken),
                clientTransport.SetEncryptionAsync(SessionEncryption.TLS, CancellationToken));
            await clientTransport.SendAsync(authenticatingSession, CancellationToken);
            var receivedEnvelopeAfterUpgrade = await serverTransport.ReceiveAsync(CancellationToken);
            
            // Assert
            serverTransport.Encryption.ShouldBe(SessionEncryption.TLS);
            clientTransport.Encryption.ShouldBe(SessionEncryption.TLS);
            var actualNewSession = receivedEnvelopeBeforeUpgrade.ShouldBeOfType<Session>();
            actualNewSession.Id.ShouldBe(newSession.Id);
            actualNewSession.State.ShouldBe(newSession.State);
            var actualAuthenticatingSession = receivedEnvelopeAfterUpgrade.ShouldBeOfType<Session>();
            actualAuthenticatingSession.Id.ShouldBe(authenticatingSession.Id);
            actualAuthenticatingSession.State.ShouldBe(authenticatingSession.State);
        }

        [Test]
        public async Task AuthenticateAsync_WithoutClientCertificate_ShouldReturnUnknown()
        {
            // Arrange
            ServerCertificate = CertificateUtil.GetOrCreateSelfSignedCertificate("localhost");
            ClientCertificateValidationCallback = ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            var clientIdentity = Dummy.CreateIdentity();
            ClientCertificate = null;
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();
            await Task.WhenAll(
                serverTransport.SetEncryptionAsync(SessionEncryption.TLS, CancellationToken),
                clientTransport.SetEncryptionAsync(SessionEncryption.TLS, CancellationToken));
            
            // Act
            var actual = await ((IAuthenticatableTransport) serverTransport).AuthenticateAsync(clientIdentity);
            
            // Assert
            actual.ShouldBe(DomainRole.Unknown);
        }        
        
        [Test]
        public async Task AuthenticateAsync_WithDifferentIdentity_ShouldReturnUnknown()
        {
            // Arrange
            ServerCertificate = CertificateUtil.GetOrCreateSelfSignedCertificate("localhost");
            ClientCertificateValidationCallback = ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            var clientIdentity = Dummy.CreateIdentity();
            var otherClientIdentity = Dummy.CreateIdentity();
            ClientCertificate = CertificateUtil.GetOrCreateSelfSignedCertificate(clientIdentity);
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();
            await Task.WhenAll(
                serverTransport.SetEncryptionAsync(SessionEncryption.TLS, CancellationToken),
                clientTransport.SetEncryptionAsync(SessionEncryption.TLS, CancellationToken));
            
            // Act
            var actual = await ((IAuthenticatableTransport) serverTransport).AuthenticateAsync(otherClientIdentity);
            
            // Assert
            actual.ShouldBe(DomainRole.Unknown);
        }
        
        [Test]
        public async Task AuthenticateAsync_WithInvalidClientCertificate_ShouldThrowAuthenticationException()
        {
            // Arrange
            ServerCertificate = CertificateUtil.GetOrCreateSelfSignedCertificate("localhost");
            ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            ClientCertificateValidationCallback = (sender, certificate, chain, errors) => false;
            var clientIdentity = Dummy.CreateIdentity();
            ClientCertificate = CertificateUtil.GetOrCreateSelfSignedCertificate(clientIdentity);
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();
            
            // Act
            await Task.WhenAll(
                serverTransport.SetEncryptionAsync(SessionEncryption.TLS, CancellationToken),
                clientTransport.SetEncryptionAsync(SessionEncryption.TLS, CancellationToken))
                .ShouldThrowAsync<AuthenticationException>();
        }

        [Test]
        public async Task SendAsync_FullSessionNegotiationWithTlsUpgrade_ShouldSucceed()
        {
            // Arrange
            ServerCertificate = CertificateUtil.GetOrCreateSelfSignedCertificate("localhost");
            ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();
            var clientChannel = new ClientChannel(clientTransport, TimeSpan.FromSeconds(30));
            var serverChannel = new ServerChannel(EnvelopeId.NewId(), "server@domain.com", serverTransport, TimeSpan.FromSeconds(30));
            
            // Act
            var clientEstablishmentTask = clientChannel.EstablishSessionAsync(
                c => SessionCompression.None,
                e => SessionEncryption.TLS,
                "client@domain.com",
                (schemes, authentication) => new GuestAuthentication(),
                EnvelopeId.NewId(),
                CancellationToken);
            var serverEstablishmentTask = serverChannel.EstablishSessionAsync(
                serverTransport.GetSupportedCompression(),
                serverTransport.GetSupportedEncryption(),
                new[] {AuthenticationScheme.Guest},
                (i, am, cancellationToken) => new AuthenticationResult(DomainRole.Member).AsCompletedTask(),
                (_, __, ___) => Task.FromResult(Node.Parse("client@domain.com/instance")),
                CancellationToken);
            await Task.WhenAll(clientEstablishmentTask, serverEstablishmentTask);

            // Assert
            serverTransport.Encryption.ShouldBe(SessionEncryption.TLS);
            clientTransport.Encryption.ShouldBe(SessionEncryption.TLS);
            var session = await clientEstablishmentTask;
            session.State.ShouldBe(SessionState.Established);
        }
    }
}
