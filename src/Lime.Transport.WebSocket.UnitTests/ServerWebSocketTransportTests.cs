using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
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
            EnvelopeSerializer = new JsonNetSerializer();
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
            var actualNotification = actual.ShouldBeOfType<Notification>();
            actualNotification.Id.ShouldBe(notification.Id);
            actualNotification.From.ShouldBe(notification.From);
            actualNotification.To.ShouldBe(notification.To);
            actualNotification.Pp.ShouldBe(notification.Pp);
            actualNotification.Metadata.ShouldBe(notification.Metadata);
            actualNotification.Event.ShouldBe(notification.Event);
            actualNotification.Reason.ShouldBe(notification.Reason);
            actualNotification.Metadata.ShouldBe(notification.Metadata);
        }

        [Test]
        public async Task SendAsync_MultipleParallelNotifications_ClientShouldReceive()
        {
            // Arrange            
            var count = Dummy.CreateRandomInt(100) + 1;
            var notifications = Enumerable.Range(0, count)
                .Select(i =>
                {
                    var notification = Dummy.CreateNotification(Event.Dispatched);
                    notification.Id = Guid.NewGuid();
                    return notification;
                })
                .ToList();
            var target = await GetTargetAsync();

            // Act
            Parallel.ForEach(notifications, async notification =>
            {
                await target.SendAsync(notification, CancellationToken);
            });


            var receiveTasks = new List<Task<Envelope>>();            
            while (count-- > 0)
            {
                receiveTasks.Add( 
                    Task.Run(async () => await Client.ReceiveAsync(CancellationToken), 
                    CancellationToken));
            }
            
            await Task.WhenAll(receiveTasks);
            var actuals = receiveTasks.Select(t => t.Result).ToList();
            
            // Assert
            actuals.Count.ShouldBe(notifications.Count);
            foreach (var notification in notifications)
            {
                var actualEnvelope = actuals.FirstOrDefault(e => e.Id == notification.Id);
                actualEnvelope.ShouldNotBeNull();
                var actualNotification = actualEnvelope.ShouldBeOfType<Notification>();
                actualNotification.Id.ShouldBe(notification.Id);
                actualNotification.From.ShouldBe(notification.From);
                actualNotification.To.ShouldBe(notification.To);
                actualNotification.Pp.ShouldBe(notification.Pp);
                actualNotification.Metadata.ShouldBe(notification.Metadata);
                actualNotification.Event.ShouldBe(notification.Event);
                actualNotification.Reason.ShouldBe(notification.Reason);
                actualNotification.Metadata.ShouldBe(notification.Metadata);
            }
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

        [Test]
        public async Task CloseAsync_ConnectedTransport_PerformClose()
        {
            // Arrange
            var target = await GetTargetAsync();
            var session = Dummy.CreateSession(SessionState.Negotiating);
            await target.SendAsync(session, CancellationToken); // Send something to assert is connected

            // Act
            await target.CloseAsync(CancellationToken);

            // Assert
            try
            {
                await target.SendAsync(session, CancellationToken); // Send something to assert is connected
                Assert.Fail("Send was succeeded but an exception was expected");
            }
            catch (Exception ex)
            {
                ex.ShouldBeOfType<InvalidOperationException>();
            }            
        }
    }
}