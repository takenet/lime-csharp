using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Messaging;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.Server;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Common.Network
{
    public abstract class ServerTransportTestsBase<TServerTransport, TClientTransport, TTransportListener>
        where TServerTransport : class, ITransport
        where TClientTransport : class, ITransport
        where TTransportListener : class, ITransportListener
    {
        protected void SetUp(Uri listenerUri)
        {
            ListenerUri = listenerUri;
            EnvelopeSerializer = new EnvelopeSerializer(new DocumentTypeResolver().WithMessagingDocuments());
            TraceWriter = new Mock<ITraceWriter>();
            CancellationToken = TimeSpan.FromSeconds(30).ToCancellationToken();
        }

        [TearDown]
        public virtual async Task TearDown()
        {
            await (Listener?.StopAsync(CancellationToken) ?? Task.CompletedTask);
            Listener = null;
            Client?.DisposeIfDisposable();
            Client = null;
        }

        public async Task<TServerTransport> GetTargetAsync()
        {
            if (Listener == null) Listener = CreateTransportListener();
            if (Client == null) Client = CreateClientTransport();

            await Listener.StartAsync(CancellationToken);
            var listenerTask = Listener.AcceptTransportAsync(CancellationToken);
            await Client.OpenAsync(ListenerUri, CancellationToken);
            var webSocketTransport = (TServerTransport)await listenerTask;
            await webSocketTransport.OpenAsync(ListenerUri, CancellationToken);
            return webSocketTransport;
        }


        protected abstract TTransportListener CreateTransportListener();

        protected abstract TClientTransport CreateClientTransport();

        public Task ServerListenerTask { get; private set; }

        public Task ClientListenerTask { get; private set; }

        public Uri ListenerUri { get; private set; }

        public TTransportListener Listener { get; private set; }

        public IEnvelopeSerializer EnvelopeSerializer { get; private set; }

        public Mock<ITraceWriter> TraceWriter { get; private set; }

        public CancellationToken CancellationToken { get; set; }

        public TClientTransport Client { get; set; }

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
        public async Task SendAsync_ConsumedNotification_ClientShouldReceive()
        {
            // Arrange            
            var notification = Dummy.CreateNotification(Event.Consumed);
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
                    var notification = Dummy.CreateNotification(Event.Consumed);
                    notification.Id = EnvelopeId.NewId();
                    return notification;
                })
                .ToList();
            var serverTransport = new SynchronizedTransportDecorator(await GetTargetAsync());
            var clientTransport = new SynchronizedTransportDecorator(Client);

            // Act
            Parallel.ForEach(notifications, async notification =>
            {
                await serverTransport.SendAsync(notification, CancellationToken);
            });


            var receiveTasks = new List<Task<Envelope>>();
            while (count-- > 0)
            {
                receiveTasks.Add(
                    Task.Run(async () => await clientTransport.ReceiveAsync(CancellationToken),
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
            var received = await Client.ReceiveAsync(CancellationToken);

            // Act
            await Task.WhenAll(
                Client.CloseAsync(CancellationToken),
                target.CloseAsync(CancellationToken));

            // Assert
            try
            {
                await target.SendAsync(session, CancellationToken); // Send something to assert is connected
                throw new Exception("Send was succeeded but an exception was expected");
            }
            catch (Exception ex)
            {
                ex.ShouldBeOfType<InvalidOperationException>();
            }
        }

        [Test]
        public async Task RemoteEndPoint_ConnectedTransport_ShouldEqualsClientLocalEndPoint()
        {
            // Arrange
            var target = await GetTargetAsync();

            // Act
            var actual = target.RemoteEndPoint;
            
            // Assert
            actual.ShouldBe(Client.LocalEndPoint);
        }
        
        [Test]
        public async Task LocalEndPoint_ConnectedTransport_ShouldEqualsClientRemoteEndPoint()
        {
            // Arrange
            var target = await GetTargetAsync();

            // Act
            var actual = target.LocalEndPoint;
            
            // Assert
            actual.ShouldBe(Client.RemoteEndPoint);
        }
        
        [Test]
        public async Task Options_ConnectedTransport_ReturnsValue()
        {
            // Arrange
            var target = await GetTargetAsync();

            // Act
            var actual = target.Options;
            
            // Assert
            actual.ShouldNotBeNull();
        }
    }
}
