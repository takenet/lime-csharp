using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Security;
using Lime.Protocol.Server;
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
            Headers = new HeaderDictionary();
            Stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"type\":\"text/plain\",\"content\":\"Hello\"}"));
            HttpRequest = new Mock<HttpRequest>();
            HttpRequest
                .SetupGet(r => r.Headers)
                .Returns(Headers);
            HttpRequest
                .SetupGet(r => r.Body)
                .Returns(Stream);
            HttpRequest
                .SetupGet(r => r.Method)
                .Returns("POST");
            HttpRequest
                .SetupGet(r => r.ContentType)
                .Returns("application/json");
            HttpRequest
                .SetupGet(r => r.Path)
                .Returns("/messages");
            HttpContext
                .SetupGet(c => c.Request)
                .Returns(HttpRequest.Object);
        }

        public HeaderDictionary Headers { get; private set; }
        public Mock<HttpRequest> HttpRequest { get; private set; }
        public MemoryStream Stream { get; private set; }

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
    }
}