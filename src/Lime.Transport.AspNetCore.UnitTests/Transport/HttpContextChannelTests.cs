using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Transport.AspNetCore.Transport;
using Lime.Transport.AspNetCore.UnitTests.Middlewares;
using NUnit.Framework;
using Shouldly;

namespace Lime.Transport.AspNetCore.UnitTests.Transport
{
    public class HttpContextChannelTests : HttpMiddlewareTestsBase
    {
        [SetUp]
        public void SetUp()
        {
            base.SetUp(new TransportEndPoint()
            {
                Transport = TransportType.Http,
                EndPoint = new IPEndPoint(IPAddress.Any, 443)
            });

            LocalNode = new Node("local", "domain.local", "local1");
            RemoteNode = new Node("remote", "domain.local", "remote1");
        }
        
        public Node LocalNode { get; private set; }
        public Node RemoteNode { get; private set; }
        
        private HttpContextChannel GetTarget() =>
            new HttpContextChannel(
                HttpContext.Object,
                LocalNode,
                RemoteNode,
                EnvelopeSerializer);

        [Test]
        public async Task SendMessage_SingleMessage_ShouldWriteOnResponseStream()
        {
            // Arrange
            var message = new Message()
            {
                Id = EnvelopeId.NewId(),
                Content = "Hello world"
            };
            var target = GetTarget();

            // Act
            await target.SendMessageAsync(message, CancellationTokenSource.Token);

            // Assert
            ResponseBody.Position.ShouldBePositive();
            ResponseBody.Position = 0;
            using var reader = new StreamReader(ResponseBody, Encoding.UTF8);
            var json = await reader.ReadToEndAsync();
            var actualEnvelope = EnvelopeSerializer.Deserialize(json);
            var actualMessage = actualEnvelope.ShouldBeOfType<Message>();
            actualMessage.Id.ShouldBe(message.Id);
            actualMessage.Type.ShouldBe(message.Type);
            actualMessage.Content.ToString().ShouldBe(message.Content.ToString());
        }

        [Test]
        public async Task SendCommand_SingleCommand_ShouldWriteOnResponseStream()
        {
            // Arrange
            var message = new Command()
            {
                Id = EnvelopeId.NewId(),
                Method = CommandMethod.Set,
                Uri = "/greeting",
                Resource = "Hello world"
            };
            var target = GetTarget();

            // Act
            await target.SendCommandAsync(message, CancellationTokenSource.Token);

            // Assert
            ResponseBody.Position.ShouldBePositive();
            ResponseBody.Position = 0;
            using var reader = new StreamReader(ResponseBody, Encoding.UTF8);
            var json = await reader.ReadToEndAsync();
            var actualEnvelope = EnvelopeSerializer.Deserialize(json);
            var actualCommand = actualEnvelope.ShouldBeOfType<Command>();
            actualCommand.Id.ShouldBe(message.Id);
            actualCommand.Method.ShouldBe(message.Method);
            actualCommand.Uri.ShouldBe(message.Uri);
            actualCommand.Type.ShouldBe(message.Type);
            actualCommand.Resource.ToString().ShouldBe(message.Resource.ToString());
        }

        [Test]
        public async Task SendNotification_SingleNotification_ShouldWriteOnResponseStream()
        {
            // Arrange
            var notification = new Notification()
            {
                Id = EnvelopeId.NewId(),
                Event = Event.Received
            };
            var target = GetTarget();

            // Act
            await target.SendNotificationAsync(notification, CancellationTokenSource.Token);

            // Assert
            ResponseBody.Position.ShouldBePositive();
            ResponseBody.Position = 0;
            using var reader = new StreamReader(ResponseBody, Encoding.UTF8);
            var json = await reader.ReadToEndAsync();
            var actualEnvelope = EnvelopeSerializer.Deserialize(json);
            var actualNotification = actualEnvelope.ShouldBeOfType<Notification>();
            actualNotification.Id.ShouldBe(notification.Id);
            actualNotification.Event.ShouldBe(notification.Event);
        }

        [Test]
        public async Task SendMessage_TwoMessages_ShouldThrowNotSupportedException()
        {
            // Arrange
            var message = new Message()
            {
                Id = EnvelopeId.NewId(),
                Content = "Hello world"
            };
            var target = GetTarget();
            await target.SendMessageAsync(message, CancellationTokenSource.Token);

            // Act
            Assert.ThrowsAsync<NotSupportedException>(() => target.SendMessageAsync(message, CancellationTokenSource.Token));
        }

        [Test]
        public async Task SendMessage_TwoEnvelopes_ShouldThrowNotSupportedException()
        {
            // Arrange
            var message = new Message()
            {
                Id = EnvelopeId.NewId(),
                Content = "Hello world"
            };
            var notification = new Notification(message.Id)
            {
                Event = Event.Received
            };
            var target = GetTarget();
            await target.SendNotificationAsync(notification, CancellationTokenSource.Token);

            // Act
            Assert.ThrowsAsync<NotSupportedException>(() => target.SendMessageAsync(message, CancellationTokenSource.Token));
        }
        
    }
}