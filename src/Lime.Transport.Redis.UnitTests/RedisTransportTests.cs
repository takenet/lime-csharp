using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Lime.Messaging;
using Lime.Messaging.Contents;
using Lime.Messaging.Resources;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.UnitTests;
using Moq;
using Shouldly;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lime.Transport.Redis.UnitTests
{
    [TestClass]
    [TestCategory(nameof(Redis))]
    public class RedisTransportTests
    {
        private  RedisFixture _redisFixture;

        static RedisTransportTests()
        {
            Registrator.RegisterDocuments();
        }

        public RedisTransportTests()
        {            
            ChannelNamespace = EnvelopeId.NewId();
            ListenerUri = new Uri("redis://localhost");
            EnvelopeSerializer = new JsonNetSerializer();
            TraceWriter = new Mock<ITraceWriter>();
            Listener = new RedisTransportListener(ListenerUri, EnvelopeSerializer, TraceWriter.Object, channelNamespace: ChannelNamespace);            
            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
            Listener.StartAsync(CancellationToken).Wait();
        }

        [TestInitialize]
        public void Start()
        {
            _redisFixture =  new RedisFixture();
        }

        [TestCleanup]
        public void Stop()
        {
            _redisFixture.Dispose();
        }

        public Uri ListenerUri { get; private set; }

        public IEnvelopeSerializer EnvelopeSerializer { get; private set; }

        public RedisTransportListener Listener { get; private set; }

        public Mock<ITraceWriter> TraceWriter { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public ITransport ServerTransport { get; private set; }

        public Process RedisProccess { get; private set; }

        public Session EstablishedSession { get; private set; }

        public string ChannelNamespace { get; private set; }


        public RedisTransport GetTarget()
        {
            return new RedisTransport(
                ListenerUri,
                new JsonNetSerializer(),
                channelNamespace: ChannelNamespace);
        }

        public async Task<RedisTransport> GetTargetAndOpenAsync()
        {           
            var transport = GetTarget();
            await transport.OpenAsync(ListenerUri, CancellationToken);
            return transport;
        }

        public async Task<RedisTransport> GetTargetAndEstablish()
        {
            var transport = await GetTargetAndOpenAsync();
            await transport.SendAsync(new Session { State = SessionState.New }, CancellationToken);
            ServerTransport = await Listener.AcceptTransportAsync(CancellationToken);
            await ServerTransport.ReceiveAsync(CancellationToken);
            EstablishedSession = Dummy.CreateSession(SessionState.Established);
            await ServerTransport.SendAsync(EstablishedSession, CancellationToken);
            await transport.ReceiveAsync(CancellationToken);
            return transport;
        }


        [TestMethod]
        public async Task SendAsync_NewSessionEnvelope_ServerShouldReceive()
        {
            // Arrange
            var session = Dummy.CreateSession(SessionState.New);
            session.Id = null;
            var target = await GetTargetAndOpenAsync();

            // Act
            await target.SendAsync(session, CancellationToken);

            // Assert
            var serverTransport = await Listener.AcceptTransportAsync(CancellationToken);
            var receivedEnvelope = await serverTransport.ReceiveAsync(CancellationToken);
            var receivedSession = receivedEnvelope.ShouldBeOfType<Session>();
            receivedSession.State.ShouldBe(SessionState.New);
        }

        [TestMethod]
        public async Task SendAsync_FinishingSessionEnvelope_ServerShouldReceive()
        {
            // Arrange            
            var target = await GetTargetAndEstablish();
            var session = Dummy.CreateSession(SessionState.Finishing);
            session.Id = EstablishedSession.Id;

            // Act
            await target.SendAsync(session, CancellationToken);

            // Assert
            var receivedEnvelope = await ServerTransport.ReceiveAsync(CancellationToken);
            var actualSession = receivedEnvelope.ShouldBeOfType<Session>();
            actualSession.State.ShouldBe(SessionState.Finishing);
        }

        [TestMethod]
        public async Task SendAsync_MessageEnvelope_ServerShouldReceive()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            var target = await GetTargetAndEstablish();

            // Act
            await target.SendAsync(message, CancellationToken);

            // Assert
            var receivedEnvelope = await ServerTransport.ReceiveAsync(CancellationToken);
            var actual = receivedEnvelope.ShouldBeOfType<Message>();
            actual.Id.ShouldBe(message.Id);
            actual.From.ShouldBe(message.From);
            actual.To.ShouldBe(message.To);
            var actualContent = actual.Content.ShouldBeOfType<PlainText>();
            actualContent.Text.ShouldBe(content.Text);
        }

        [TestMethod]
        public async Task SendAsync_NotificationEnvelope_ServerShouldReceive()
        {
            // Arrange            
            var notification = Dummy.CreateNotification(Event.Received);
            var target = await GetTargetAndEstablish();

            // Act
            await target.SendAsync(notification, CancellationToken);

            // Assert
            var receivedEnvelope = await ServerTransport.ReceiveAsync(CancellationToken);
            var actual = receivedEnvelope.ShouldBeOfType<Notification>();
            actual.Id.ShouldBe(notification.Id);
            actual.From.ShouldBe(notification.From);
            actual.To.ShouldBe(notification.To);
            actual.Event.ShouldBe(notification.Event);
        }

        [TestMethod]
        public async Task SendAsync_CommandEnvelope_ServerShouldReceive()
        {
            // Arrange
            var presence = Dummy.CreatePresence();
            var command = Dummy.CreateCommand(presence);
            var target = await GetTargetAndEstablish();

            // Act
            await target.SendAsync(command, CancellationToken);

            // Assert
            var receivedEnvelope = await ServerTransport.ReceiveAsync(CancellationToken);
            var actual = receivedEnvelope.ShouldBeOfType<Command>();
            actual.Id.ShouldBe(command.Id);
            actual.From.ShouldBe(command.From);
            actual.To.ShouldBe(command.To);
            var actualResource = actual.Resource.ShouldBeOfType<Presence>();
            actualResource.Status.ShouldBe(presence.Status);
            actualResource.Message.ShouldBe(presence.Message);
            actualResource.RoutingRule.ShouldBe(presence.RoutingRule);
            actualResource.Priority.ShouldBe(presence.Priority);
        }

        [TestMethod]
        public async Task ReceiveAsync_MessageEnvelope_ClientShouldReceive()
        {
            // Arrange
            var content = Dummy.CreateTextContent();
            var message = Dummy.CreateMessage(content);
            var target = await GetTargetAndEstablish();
            await ServerTransport.SendAsync(message, CancellationToken);

            // Act
            var receivedEnvelope = await target.ReceiveAsync(CancellationToken);

            // Assert
            var actual = receivedEnvelope.ShouldBeOfType<Message>();
            actual.Id.ShouldBe(message.Id);
            actual.From.ShouldBe(message.From);
            actual.To.ShouldBe(message.To);
            var actualContent = actual.Content.ShouldBeOfType<PlainText>();
            actualContent.Text.ShouldBe(content.Text);
        }

        [TestMethod]
        public async Task ReceiveAsync_NotificationEnvelope_ClientShouldReceive()
        {
            // Arrange            
            var notification = Dummy.CreateNotification(Event.Received);
            var target = await GetTargetAndEstablish();
            await ServerTransport.SendAsync(notification, CancellationToken);

            // Act
            var receivedEnvelope = await target.ReceiveAsync(CancellationToken);

            // Assert
            var actual = receivedEnvelope.ShouldBeOfType<Notification>();
            actual.Id.ShouldBe(notification.Id);
            actual.From.ShouldBe(notification.From);
            actual.To.ShouldBe(notification.To);
            actual.Event.ShouldBe(notification.Event);
        }

        [TestMethod]
        public async Task ReceiveAsync_CommandEnvelope_ClientShouldReceive()
        {
            // Arrange
            var presence = Dummy.CreatePresence();
            var command = Dummy.CreateCommand(presence);
            var target = await GetTargetAndEstablish();
            await ServerTransport.SendAsync(command, CancellationToken);

            // Act
            var receivedEnvelope = await target.ReceiveAsync(CancellationToken);
            

            // Assert            
            var actual = receivedEnvelope.ShouldBeOfType<Command>();
            actual.Id.ShouldBe(command.Id);
            actual.From.ShouldBe(command.From);
            actual.To.ShouldBe(command.To);
            var actualResource = actual.Resource.ShouldBeOfType<Presence>();
            actualResource.Status.ShouldBe(presence.Status);
            actualResource.Message.ShouldBe(presence.Message);
            actualResource.RoutingRule.ShouldBe(presence.RoutingRule);
            actualResource.Priority.ShouldBe(presence.Priority);
        }

        public void Dispose()
        {
            Listener.StopAsync(CancellationToken).Wait();
            Listener.Dispose();
        }
    }
}
