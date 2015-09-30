using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.UnitTests;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Transport.WebSocket.UnitTests
{
    [TestFixture]
    public class ServerWebSocketTransportTests
    {
        public async Task<ServerWebSocketTransport> GetTargetAsync()
        {
            await Listener.StartAsync();
            await Client.OpenAsync(ListenerUri, CancellationToken);
            return (ServerWebSocketTransport)await Listener.AcceptTransportAsync(CancellationToken);
        }

        public Uri ListenerUri { get; private set; }

        public WebSocketTransportListener Listener { get; private set; }

        public X509Certificate2 SslCertificate { get; private set; }

        public IEnvelopeSerializer EnvelopeSerializer { get; private set; }

        public Mock<ITraceWriter> TraceWriter { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public ClientWebSocketTransport Client { get; set; }

        [SetUp]
        public void SetUp()
        {
            ListenerUri = new Uri("ws://localhost:8081");
            EnvelopeSerializer = new EnvelopeSerializer();
            TraceWriter = new Mock<ITraceWriter>();
            Listener = new WebSocketTransportListener(ListenerUri, SslCertificate, EnvelopeSerializer, TraceWriter.Object);
            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
            Client = new ClientWebSocketTransport(EnvelopeSerializer);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Listener.StopAsync().Wait();
            }
            catch (AggregateException) { }
        }

        [Test]
        public async Task SendAsync_EstablishedSessionEnvelope_ClientShouldReceive()
        {
            // Arrange            
            var session = Dummy.CreateSession(SessionState.Established);
            var target = await GetTargetAsync();

            // Act
            await target.SendAsync(session, CancellationToken);
            var actual = await Client.ReceiveAsync(CancellationToken);

            // Assert
            actual.ShouldNotBeNull();
            var actualSession = actual.ShouldBeOfType<Session>();
            actualSession.Id.ShouldBe(session.Id);
            actualSession.From.ShouldBe(session.From);
            actualSession.To.ShouldBe(session.To);
            actualSession.Pp.ShouldBe(session.Pp);
            actualSession.Metadata.ShouldBe(session.Metadata);
            actualSession.State.ShouldBe(session.State);
            actualSession.Scheme.ShouldBe(session.Scheme);
            actualSession.SchemeOptions.ShouldBe(session.SchemeOptions);
            actualSession.Authentication.ShouldBe(session.Authentication);
            actualSession.Compression.ShouldBe(session.Compression);
            actualSession.CompressionOptions.ShouldBe(session.CompressionOptions);
            actualSession.Encryption.ShouldBe(session.Encryption);
            actualSession.EncryptionOptions.ShouldBe(session.EncryptionOptions);
            actualSession.Reason.ShouldBe(session.Reason);
            actualSession.Metadata.ShouldBe(session.Metadata);
        }

        [Test]
        public async Task SendAsync_FullSessionEnvelope_ClientShouldReceive()
        {
            // Arrange            
            var session = Dummy.CreateSession(SessionState.Negotiating);
            var plainAuthentication = Dummy.CreatePlainAuthentication();
            session.Authentication = plainAuthentication;
            session.Compression = SessionCompression.GZip;
            session.CompressionOptions = new[] { SessionCompression.GZip, SessionCompression.None };
            session.Encryption = SessionEncryption.TLS;
            session.EncryptionOptions = new[] { SessionEncryption.TLS, SessionEncryption.None };
            session.Reason = Dummy.CreateReason();
            session.Metadata = Dummy.CreateStringStringDictionary();
            var target = await GetTargetAsync();

            // Act
            await target.SendAsync(session, CancellationToken);
            var actual = await Client.ReceiveAsync(CancellationToken);

            // Assert
            actual.ShouldNotBeNull();
            var actualSession = actual.ShouldBeOfType<Session>();
            actualSession.Id.ShouldBe(session.Id);
            actualSession.From.ShouldBe(session.From);
            actualSession.To.ShouldBe(session.To);
            actualSession.Pp.ShouldBe(session.Pp);
            actualSession.State.ShouldBe(session.State);
            actualSession.Scheme.ShouldBe(session.Scheme);
            actualSession.SchemeOptions.ShouldBe(session.SchemeOptions);
            var actualSessionAuthentication = actualSession.Authentication.ShouldBeOfType<PlainAuthentication>();
            actualSessionAuthentication.Password.ShouldBe(plainAuthentication.Password);
            actualSession.Compression.ShouldBe(session.Compression);
            actualSession.CompressionOptions.ShouldBe(session.CompressionOptions);
            actualSession.Encryption.ShouldBe(session.Encryption);
            actualSession.EncryptionOptions.ShouldBe(session.EncryptionOptions);
            actualSession.Reason.ShouldNotBeNull();
            actualSession.Reason.Description.ShouldBe(session.Reason.Description);
            actualSession.Reason.Code.ShouldBe(session.Reason.Code);
            actualSession.Metadata.ShouldBe(session.Metadata);
        }

        [Test]
        public async Task SendAsync_DispatchedNotification_ClientShouldReceive()
        {
            // Arrange            
            var notification = Dummy.CreateNotification(Event.Dispatched);
            var target = await GetTargetAsync();

            // Act
            await target.SendAsync(notification, CancellationToken);
            var actual = await Client.ReceiveAsync(CancellationToken);

            // Assert
            actual.ShouldNotBeNull();
            var actualSession = actual.ShouldBeOfType<Notification>();
            actualSession.Id.ShouldBe(notification.Id);
            actualSession.From.ShouldBe(notification.From);
            actualSession.To.ShouldBe(notification.To);
            actualSession.Pp.ShouldBe(notification.Pp);
            actualSession.Metadata.ShouldBe(notification.Metadata);
            actualSession.Event.ShouldBe(notification.Event);
            actualSession.Reason.ShouldBe(notification.Reason);
            actualSession.Metadata.ShouldBe(notification.Metadata);
        }

        [Test]
        public async Task ReceiveAsync_NewSessionEnvelope_ServerShouldReceive()
        {
            // Arrange            
            var session = Dummy.CreateSession(SessionState.New);
            var target = await GetTargetAsync();

            // Act
            await Client.SendAsync(session, CancellationToken);
            var actual = await target.ReceiveAsync(CancellationToken);

            // Assert
            actual.ShouldNotBeNull();
            var actualSession = actual.ShouldBeOfType<Session>();
            actualSession.Id.ShouldBe(session.Id);
            actualSession.From.ShouldBe(session.From);
            actualSession.To.ShouldBe(session.To);
            actualSession.Pp.ShouldBe(session.Pp);
            actualSession.Metadata.ShouldBe(session.Metadata);
            actualSession.State.ShouldBe(session.State);
            actualSession.Scheme.ShouldBe(session.Scheme);
            actualSession.SchemeOptions.ShouldBe(session.SchemeOptions);
            actualSession.Authentication.ShouldBe(session.Authentication);
            actualSession.Compression.ShouldBe(session.Compression);
            actualSession.CompressionOptions.ShouldBe(session.CompressionOptions);
            actualSession.Encryption.ShouldBe(session.Encryption);
            actualSession.EncryptionOptions.ShouldBe(session.EncryptionOptions);
            actualSession.Reason.ShouldBe(session.Reason);
            actualSession.Metadata.ShouldBe(session.Metadata);
        }

        [Test]
        public async Task ReceiveAsync_FullSessionEnvelope_ServerShouldReceive()
        {
            // Arrange            
            var session = Dummy.CreateSession(SessionState.Negotiating);
            var plainAuthentication = Dummy.CreatePlainAuthentication();
            session.Authentication = plainAuthentication;
            session.Compression = SessionCompression.GZip;
            session.CompressionOptions = new[] { SessionCompression.GZip, SessionCompression.None };
            session.Encryption = SessionEncryption.TLS;
            session.EncryptionOptions = new[] { SessionEncryption.TLS, SessionEncryption.None };
            session.Reason = Dummy.CreateReason();
            session.Metadata = Dummy.CreateStringStringDictionary();
            var target = await GetTargetAsync();

            // Act
            await Client.SendAsync(session, CancellationToken);
            var actual = await target.ReceiveAsync(CancellationToken);

            // Assert
            actual.ShouldNotBeNull();
            var actualSession = actual.ShouldBeOfType<Session>();
            actualSession.Id.ShouldBe(session.Id);
            actualSession.From.ShouldBe(session.From);
            actualSession.To.ShouldBe(session.To);
            actualSession.Pp.ShouldBe(session.Pp);
            actualSession.State.ShouldBe(session.State);
            actualSession.Scheme.ShouldBe(session.Scheme);
            actualSession.SchemeOptions.ShouldBe(session.SchemeOptions);
            var actualSessionAuthentication = actualSession.Authentication.ShouldBeOfType<PlainAuthentication>();
            actualSessionAuthentication.Password.ShouldBe(plainAuthentication.Password);
            actualSession.Compression.ShouldBe(session.Compression);
            actualSession.CompressionOptions.ShouldBe(session.CompressionOptions);
            actualSession.Encryption.ShouldBe(session.Encryption);
            actualSession.EncryptionOptions.ShouldBe(session.EncryptionOptions);
            actualSession.Reason.ShouldNotBeNull();
            actualSession.Reason.Description.ShouldBe(session.Reason.Description);
            actualSession.Reason.Code.ShouldBe(session.Reason.Code);
            actualSession.Metadata.ShouldBe(session.Metadata);
        }

    }
}