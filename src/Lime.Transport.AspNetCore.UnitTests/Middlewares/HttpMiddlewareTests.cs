using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Security;
using Lime.Protocol.Server;
using Lime.Transport.AspNetCore.Listeners;
using Lime.Transport.AspNetCore.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    [TestFixture]
    public class HttpMiddlewareTests : HttpMiddlewareTestsBase
    {
        [SetUp]
        public void SetUp()
        {
            base.SetUp(new TransportEndPoint()
            {
                Transport = TransportType.Http,
                EndPoint = new IPEndPoint(IPAddress.Any, 443)
            });


        }


        
        private HttpMiddleware GetTarget() => new HttpMiddleware(
            RequestDelegateExecutor.Next,
            EnvelopeSerializer,
            Microsoft.Extensions.Options.Options.Create(Options),
            TransportListener,
            new Logger<HttpMiddleware>(new LoggerFactory()));
        
        [Test]
        public async Task Invoke_WithoutAuthenticationHeader_ShouldAuthenticateAsGuest()
        {
            // Arrange
            Identity handlerIdentity = null;
            Authentication handlerAuthentication = null;
            var handlerResult = new AuthenticationResult(DomainRole.Member);
            Options.AuthenticationHandler = (identity, authentication, token) =>
            {
                handlerIdentity = identity;
                handlerAuthentication = authentication;
                return Task.FromResult(handlerResult);
            };
            RequestBody.Write(Encoding.UTF8.GetBytes("{\"type\":\"text/plain\",\"content\":\"Hello\"}"));
            RequestBody.Position = 0;
            var target = GetTarget();

            // Act
            await target.Invoke(HttpContext.Object);

            // Assert
            handlerIdentity.ShouldNotBeNull();
            handlerAuthentication.ShouldNotBeNull();
            handlerAuthentication.ShouldBeOfType<GuestAuthentication>();
        }
        
        [Test]
        public async Task Invoke_WithBasicAuthenticationHeader_ShouldAuthenticateAsPlain()
        {
            // Arrange
            Identity handlerIdentity = null;
            Authentication handlerAuthentication = null;
            var handlerResult = new AuthenticationResult(DomainRole.Member);
            Options.AuthenticationHandler = (identity, authentication, token) =>
            {
                handlerIdentity = identity;
                handlerAuthentication = authentication;
                return Task.FromResult(handlerResult);
            };
            var userName = "name@domain.local";
            var password = "(*R1jsd92<asÇ2931kd";
            var authorizationValue = $"{userName}:{password}".ToBase64();
            Headers.Add(HeaderNames.Authorization, $"Basic {authorizationValue}");
            RequestBody.Write(Encoding.UTF8.GetBytes("{\"type\":\"text/plain\",\"content\":\"Hello\"}"));
            RequestBody.Position = 0;
            var target = GetTarget();

            // Act
            await target.Invoke(HttpContext.Object);

            // Assert
            handlerIdentity.ShouldNotBeNull();
            handlerIdentity.ToString().ShouldBe(userName);
            handlerAuthentication.ShouldNotBeNull();
            var plainAuthentication = handlerAuthentication.ShouldBeOfType<PlainAuthentication>();
            plainAuthentication.GetFromBase64Password().ShouldBe(password);
        }
        
        [Test]
        public async Task Invoke_WithKeyAuthenticationHeader_ShouldAuthenticateAsPlain()
        {
            // Arrange
            Identity handlerIdentity = null;
            Authentication handlerAuthentication = null;
            var handlerResult = new AuthenticationResult(DomainRole.Member);
            Options.AuthenticationHandler = (identity, authentication, token) =>
            {
                handlerIdentity = identity;
                handlerAuthentication = authentication;
                return Task.FromResult(handlerResult);
            };
            var userName = "name@domain.local";
            var password = "(*R1jsd92<asÇ2931kd";
            var authorizationValue = $"{userName}:{password}".ToBase64();
            Headers.Add(HeaderNames.Authorization, $"Key {authorizationValue}");
            RequestBody.Write(Encoding.UTF8.GetBytes("{\"type\":\"text/plain\",\"content\":\"Hello\"}"));
            RequestBody.Position = 0;
            var target = GetTarget();

            // Act
            await target.Invoke(HttpContext.Object);

            // Assert
            handlerIdentity.ShouldNotBeNull();
            handlerIdentity.ToString().ShouldBe(userName);
            handlerAuthentication.ShouldNotBeNull();
            var keyAuthentication = handlerAuthentication.ShouldBeOfType<KeyAuthentication>();
            keyAuthentication.GetFromBase64Key().ShouldBe(password);
        }
        
        [Test]
        public async Task Invoke_MessageRequest_ShouldCallListener()
        {
            // Arrange
            var listener = new MockMessageListener();
            ServiceProvider
                .Setup(s => s.GetService(typeof(IEnumerable<IMessageListener>)))
                .Returns(new[] {listener});
            HttpRequest
                .SetupGet(r => r.Path)
                .Returns("/messages");
            var messageBytes = Encoding.UTF8.GetBytes("{\"type\":\"text/plain\",\"content\":\"Hello world!\"}");
            RequestBody.Write(messageBytes);
            RequestBody.Position = 0;
            
            var target = GetTarget();

            // Act
            await target.Invoke(HttpContext.Object);

            // Assert
            listener.Envelopes.Count.ShouldBe(1);
            listener.Envelopes[0].Type.ToString().ShouldBe("text/plain");
            listener.Envelopes[0].Content.ToString().ShouldBe("Hello world!");
        }
        
        [Test]
        public async Task Invoke_NotificationRequest_ShouldCallListener()
        {
            // Arrange
            var listener = new MockNotificationListener();
            ServiceProvider
                .Setup(s => s.GetService(typeof(IEnumerable<INotificationListener>)))
                .Returns(new[] {listener});
            HttpRequest
                .SetupGet(r => r.Path)
                .Returns("/notifications");
            var notificationBytes = Encoding.UTF8.GetBytes("{\"id\":\"1\",\"event\":\"received\"}");
            RequestBody.Write(notificationBytes);
            RequestBody.Position = 0;
            
            var target = GetTarget();

            // Act
            await target.Invoke(HttpContext.Object);

            // Assert
            listener.Envelopes.Count.ShouldBe(1);
            listener.Envelopes[0].Id.ShouldBe("1");
            listener.Envelopes[0].Event.ShouldBe(Event.Received);
        }     
        
        [Test]
        public async Task Invoke_CommandRequest_ShouldCallListener()
        {
            // Arrange
            var listener = new MockCommandListener();
            ServiceProvider
                .Setup(s => s.GetService(typeof(IEnumerable<ICommandListener>)))
                .Returns(new[] {listener});
            HttpRequest
                .SetupGet(r => r.Path)
                .Returns("/commands");
            var commandBytes = Encoding.UTF8.GetBytes("{\"id\":\"1\",\"method\":\"get\",\"uri\":\"/ping\"}");
            RequestBody.Write(commandBytes);
            RequestBody.Position = 0;
            
            var target = GetTarget();

            // Act
            await target.Invoke(HttpContext.Object);

            // Assert
            listener.Envelopes.Count.ShouldBe(1);
            listener.Envelopes[0].Id.ShouldBe("1");
            listener.Envelopes[0].Method.ShouldBe(CommandMethod.Get);
            listener.Envelopes[0].Uri.ToString().ShouldBe("/ping");
        }
    }
}