using System;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.UnitTests;
using Lime.Transport.Http;
using Lime.Transport.Http.Processors;
using Lime.Transport.Http.Storage;
using NUnit.Framework;
using Moq;
using Shouldly;

namespace Lime.Transport.Http.UnitTests.Processors
{
    [TestFixture]
    public class GetEnvelopesHttpProcessorTests
    {
        public Mock<IEnvelopeStorage<Envelope>> EnvelopeStorage { get; set; }

        public Mock<IPrincipal> Principal { get; set; }

        public Identity Identity { get; set; }

        public string PrincipalIdentityName { get; set; }

        public Mock<System.Security.Principal.IIdentity> PrincipalIdentity { get; set; }

        public Uri GetMessagesUri { get; set; }

        public HttpRequest GetMessagesHttpRequest { get; set; }

        public UriTemplateMatch GetMessagesUriTemplateMatch { get; set; }

        public string[] EnvelopeIds { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public GetEnvelopesHttpProcessor<Envelope> Target { get; set; }

        [SetUp]
        public void Arrange()
        {
            EnvelopeStorage = new Mock<IEnvelopeStorage<Envelope>>();
            Principal = new Mock<IPrincipal>();
            PrincipalIdentity = new Mock<System.Security.Principal.IIdentity>();
            Principal.SetupGet(p => p.Identity).Returns(() => PrincipalIdentity.Object);
            Identity = Dummy.CreateIdentity();
            PrincipalIdentityName = Identity.ToString();
            PrincipalIdentity.SetupGet(p => p.Name).Returns(() => PrincipalIdentityName);       
            GetMessagesUri = new Uri("http://" + Constants.MESSAGES_PATH + ":" + Dummy.CreateRandomInt(50000) + "/" + Constants.MESSAGES_PATH);
            GetMessagesHttpRequest = new HttpRequest("DELETE", GetMessagesUri, Principal.Object, Guid.NewGuid());
            GetMessagesUriTemplateMatch = new UriTemplateMatch();
            EnvelopeIds = new[]
            {
                EnvelopeId.NewId(),
                EnvelopeId.NewId(),
                EnvelopeId.NewId()
            };

            Target = new GetEnvelopesHttpProcessor<Envelope>(EnvelopeStorage.Object, Constants.MESSAGES_PATH);
        }

        [Test]
        public async Task ProcessAsync_ValidHttpRequest_CallsStorageAndReturnsOKHttpResponse()
        {
            // Arrange
            EnvelopeStorage
                .Setup(m => m.GetEnvelopesAsync(Identity))
                .ReturnsAsync(EnvelopeIds)
                .Verifiable();

            // Act
            var actual = await Target.ProcessAsync(GetMessagesHttpRequest, GetMessagesUriTemplateMatch, It.IsAny<ITransportSession>(), CancellationToken);

            // Assert
            actual.StatusCode.ShouldBe(HttpStatusCode.OK);
            actual.BodyStream.ShouldNotBe(null);
            var reader = new StreamReader(actual.BodyStream);
            foreach (var messageId in EnvelopeIds)
            {
                reader.ReadLine().ShouldBe(messageId.ToString());
            }
            actual.ContentType.ShouldNotBe(null);
            actual.ContentType.ToString().ShouldBe(Constants.TEXT_PLAIN_HEADER_VALUE);
            EnvelopeStorage.Verify();
        }

        [Test]
        public async Task ProcessAsync_NoStoredEnvelopesForIdentity_ReturnsNoContentHttpResponse()
        {
            // Arrange
            EnvelopeStorage
                .Setup(m => m.GetEnvelopesAsync(Identity))
                .ReturnsAsync(new string[0])
                .Verifiable();

            // Act
            var actual = await Target.ProcessAsync(GetMessagesHttpRequest, GetMessagesUriTemplateMatch, It.IsAny<ITransportSession>(), CancellationToken);

            // Assert
            actual.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            actual.BodyStream.ShouldBe(null);
            actual.ContentType.ShouldBe(null);
            EnvelopeStorage.Verify();
        }

        [Test]
        public async Task ProcessAsync_InvalidPrincipalNameFormat_RetunsBadRequestHttpResponse()
        {
            // Arrange
            PrincipalIdentityName = string.Empty;

            // Act
            var actual = await Target.ProcessAsync(GetMessagesHttpRequest, GetMessagesUriTemplateMatch, It.IsAny<ITransportSession>(), CancellationToken);

            // Assert
            actual.CorrelatorId.ShouldBe(GetMessagesHttpRequest.CorrelatorId);
            actual.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            EnvelopeStorage.Verify();
        }
    }
}
