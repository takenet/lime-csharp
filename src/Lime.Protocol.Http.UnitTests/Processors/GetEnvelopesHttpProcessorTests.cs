using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lime.Protocol.Http.Processors;
using Moq;
using Lime.Protocol.Http.Storage;
using Lime.Protocol.UnitTests;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using System.Net;
using System.IO;

namespace Lime.Protocol.Http.UnitTests.Processors
{
    [TestClass]
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

        public Guid[] EnvelopeIds { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public GetEnvelopesHttpProcessor<Envelope> Target { get; set; }

        [TestInitialize]
        public void Arrange()
        {
            EnvelopeStorage = new Mock<IEnvelopeStorage<Envelope>>();
            Principal = new Mock<IPrincipal>();
            PrincipalIdentity = new Mock<System.Security.Principal.IIdentity>();
            Principal.SetupGet(p => p.Identity).Returns(() => PrincipalIdentity.Object);
            Identity = DataUtil.CreateIdentity();
            PrincipalIdentityName = Identity.ToString();
            PrincipalIdentity.SetupGet(p => p.Name).Returns(() => PrincipalIdentityName);       
            GetMessagesUri = new Uri("http://" + Constants.MESSAGES_PATH + ":" + DataUtil.CreateRandomInt(50000) + "/" + Constants.MESSAGES_PATH);
            GetMessagesHttpRequest = new HttpRequest("DELETE", GetMessagesUri, Principal.Object, Guid.NewGuid());
            GetMessagesUriTemplateMatch = new UriTemplateMatch();
            EnvelopeIds = new Guid[]
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            Target = new GetEnvelopesHttpProcessor<Envelope>(EnvelopeStorage.Object, Constants.MESSAGES_PATH);
        }

        [TestMethod]
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
            actual.Body.ShouldNotBe(null);
            var reader = new StringReader(actual.Body);            
            foreach (var messageId in EnvelopeIds)
            {
                reader.ReadLine().ShouldBe(messageId.ToString());
            }
            actual.ContentType.ShouldNotBe(null);
            actual.ContentType.ToString().ShouldBe(Constants.TEXT_PLAIN_HEADER_VALUE);
            EnvelopeStorage.Verify();
        }

        [TestMethod]
        public async Task ProcessAsync_NoStoredEnvelopesForIdentity_ReturnsNoContentHttpResponse()
        {
            // Arrange
            EnvelopeStorage
                .Setup(m => m.GetEnvelopesAsync(Identity))
                .ReturnsAsync(new Guid[0])
                .Verifiable();

            // Act
            var actual = await Target.ProcessAsync(GetMessagesHttpRequest, GetMessagesUriTemplateMatch, It.IsAny<ITransportSession>(), CancellationToken);

            // Assert
            actual.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            actual.Body.ShouldBe(null);
            actual.ContentType.ShouldBe(null);
            EnvelopeStorage.Verify();
        }

        [TestMethod]
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
