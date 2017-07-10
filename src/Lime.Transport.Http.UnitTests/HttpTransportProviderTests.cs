using System;
using System.Net;
using System.Security.Principal;
using System.Threading;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.UnitTests;
using Lime.Transport.Http;
using Lime.Transport.Http.Storage;
using Moq;
using Shouldly;
using Xunit;

namespace Lime.Transport.Http.UnitTests
{
    public class HttpTransportProviderTests
    {
        public HttpTransportProviderTests()
        {
            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();

            UseHttps = true;

            MessageStorage = new Mock<IEnvelopeStorage<Message>>();
            NotificationStorage = new Mock<IEnvelopeStorage<Notification>>();

            Identity = Dummy.CreateIdentity();
            Password = Dummy.CreateRandomString(12);
            HttpListenerBasicIdentity = new HttpListenerBasicIdentity(Identity.ToString(), Password);
            Principal = new Mock<IPrincipal>();
            Principal.SetupGet(p => p.Identity).Returns(HttpListenerBasicIdentity);

            CacheInstance = true;
            ExpirationInactivityInterval = TimeSpan.FromSeconds(30);
            ExpirationTimerInterval = TimeSpan.FromSeconds(5);

            Target = new Lazy<HttpTransportProvider>(() => new HttpTransportProvider(UseHttps, MessageStorage.Object, NotificationStorage.Object, ExpirationInactivityInterval, ExpirationTimerInterval, CloseTransportTimeout));
        }

        public CancellationToken CancellationToken { get; private set; }

        public bool UseHttps { get; set; }

        public Mock<IEnvelopeStorage<Message>> MessageStorage { get; set; }

        public Mock<IEnvelopeStorage<Notification>> NotificationStorage { get; set; }

        public Identity Identity { get; set; }

        public string Password { get; set; }
        
        public HttpListenerBasicIdentity HttpListenerBasicIdentity { get; set; }

        public Mock<IPrincipal> Principal { get; set; }

        public bool CacheInstance { get; set; }

        public TimeSpan ExpirationInactivityInterval { get; set; }

        public TimeSpan ExpirationTimerInterval { get; set; }

        public TimeSpan CloseTransportTimeout { get; set; }

        public Lazy<HttpTransportProvider> Target { get; set; }

        [Fact]
        public void GetTransport_NewSessionCacheInstance_ReturnsAndCacheTransport()
        {
            // Act
            var first = Target.Value.GetTransport(Principal.Object, CacheInstance);
            var second = Target.Value.GetTransport(Principal.Object, CacheInstance);

            // Assert
            second.ShouldBe(first);
        }

        [Fact]
        public void GetTransport_CacheInstanceAndCloseTransport_ReturnsNewTransport()
        {
            // Act
            var first = Target.Value.GetTransport(Principal.Object, CacheInstance);
            ((ITransport)first).CloseAsync(CancellationToken).Wait();
            var second = Target.Value.GetTransport(Principal.Object, CacheInstance);

            // Assert
            second.ShouldNotBe(first);
        }

        [Fact]
        public void GetTransport_DoNotCacheInstance_ReturnsNewTransport()
        {
            CacheInstance = false;

            // Act
            var first = Target.Value.GetTransport(Principal.Object, CacheInstance);
            var second = Target.Value.GetTransport(Principal.Object, CacheInstance);

            // Assert
            second.ShouldNotBe(first);
        }

        [Fact]
        public void GetTransport_CacheFirstAndDoNotCacheAfter_ReturnsCachedAndNewTransport()
        {
            // Act
            var first = Target.Value.GetTransport(Principal.Object, true);
            var second = Target.Value.GetTransport(Principal.Object, false);
            var third = Target.Value.GetTransport(Principal.Object, false);

            // Assert
            second.ShouldBe(first);
            third.ShouldNotBe(second);
        }

        [Fact]
        public void ExpirationTimer_Elapsed_ExpiredTransport_CloseTransport()
        {
            // Arrange
            ExpirationInactivityInterval = TimeSpan.FromMilliseconds(60);
            ExpirationTimerInterval = TimeSpan.FromMilliseconds(50);
            CloseTransportTimeout = TimeSpan.FromMilliseconds(250);

            bool closed = false;

            // Act
            var actual = Target.Value.GetTransport(Principal.Object, true);
            ((ITransport)actual).Closed += (sender, e) => closed = true;
            
            Thread.Sleep(750);

            // Assert          
            closed.ShouldBe(true);
            var transport = Target.Value.GetTransport(Principal.Object, true);
            transport.ShouldNotBe(actual);
        }
    }
}
