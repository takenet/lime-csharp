using System;
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
    public class DeleteEnvelopeByIdHttpProcessorTests
    {

        public Mock<IEnvelopeStorage<Message>> MessageEnvelopeStorage { get; set; }

        public Mock<IPrincipal> Principal { get; set; }

        public Identity Identity { get; set; }

        public string PrincipalIdentityName { get; set; }

        public Mock<System.Security.Principal.IIdentity> PrincipalIdentity { get; set; }
        
        public string MessageId { get; set; }

        public Uri DeleteMessageUri { get; set; }
        
        public HttpRequest DeleteMessageHttpRequest { get; set; }

        public UriTemplateMatch DeleteMessageUriTemplateMatch { get; set; }

        public DeleteEnvelopeByIdHttpProcessor<Message> Target { get; set; }

        [SetUp]
        public void Arrange()
        {
            MessageEnvelopeStorage = new Mock<IEnvelopeStorage<Message>>();
            Principal = new Mock<IPrincipal>();
            PrincipalIdentity = new Mock<System.Security.Principal.IIdentity>();
            Principal.SetupGet(p => p.Identity).Returns(() => PrincipalIdentity.Object);
            Identity = Dummy.CreateIdentity();
            PrincipalIdentityName = Identity.ToString();
            PrincipalIdentity.SetupGet(p => p.Name).Returns(() => PrincipalIdentityName);                        
            MessageId = EnvelopeId.NewId();
            DeleteMessageUri = new Uri("http://" + Constants.MESSAGES_PATH + ":" + Dummy.CreateRandomInt(50000) + "/" + MessageId);
            DeleteMessageHttpRequest = new HttpRequest("DELETE", DeleteMessageUri, Principal.Object, Guid.NewGuid().ToString());
            DeleteMessageUriTemplateMatch = new UriTemplateMatch();
            DeleteMessageUriTemplateMatch.BoundVariables.Add("id", MessageId.ToString());
            Target = new DeleteEnvelopeByIdHttpProcessor<Message>(MessageEnvelopeStorage.Object, Constants.MESSAGES_PATH);
        }

        [Test]
        public async Task ProcessAsync_ValidHttpRequest_RetunsOKHttpResponse()
        {
            // Arrange
            MessageEnvelopeStorage
                .Setup(m => m.DeleteEnvelopeAsync(Identity, MessageId))
                .ReturnsAsync(true)
                .Verifiable();

            // Act
            var actual = await Target.ProcessAsync(DeleteMessageHttpRequest, DeleteMessageUriTemplateMatch, It.IsAny<ITransportSession>(), CancellationToken.None);

            // Assert
            actual.CorrelatorId.ShouldBe(DeleteMessageHttpRequest.CorrelatorId);
            actual.StatusCode.ShouldBe(HttpStatusCode.OK);            
            MessageEnvelopeStorage.Verify();
        }

        [Test]
        public async Task ProcessAsync_UnknownEnvelopeId_RetunsNotFoundHttpResponse()
        {
            // Arrange
            MessageEnvelopeStorage
                .Setup(m => m.DeleteEnvelopeAsync(Identity, MessageId))
                .ReturnsAsync(false)
                .Verifiable();

            // Act
            var actual = await Target.ProcessAsync(DeleteMessageHttpRequest, DeleteMessageUriTemplateMatch, It.IsAny<ITransportSession>(), CancellationToken.None);

            // Assert
            actual.CorrelatorId.ShouldBe(DeleteMessageHttpRequest.CorrelatorId);
            actual.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            MessageEnvelopeStorage.Verify();
        }

        [Test]
        public async Task ProcessAsync_RequestUriWithoutId_RetunsBadRequestHttpResponse()
        {
            // Arrange
            DeleteMessageUriTemplateMatch.BoundVariables.Clear();

            // Act
            var actual = await Target.ProcessAsync(DeleteMessageHttpRequest, DeleteMessageUriTemplateMatch, It.IsAny<ITransportSession>(), CancellationToken.None);

            // Assert
            actual.CorrelatorId.ShouldBe(DeleteMessageHttpRequest.CorrelatorId);
            actual.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            MessageEnvelopeStorage.Verify();
        }

        [Test]
        public async Task ProcessAsync_InvalidPrincipalNameFormat_RetunsBadRequestHttpResponse()
        {
            // Arrange
            PrincipalIdentityName = string.Empty;

            // Act
            var actual = await Target.ProcessAsync(DeleteMessageHttpRequest, DeleteMessageUriTemplateMatch, It.IsAny<ITransportSession>(), CancellationToken.None);

            // Assert
            actual.CorrelatorId.ShouldBe(DeleteMessageHttpRequest.CorrelatorId);
            actual.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            MessageEnvelopeStorage.Verify();
        }
    }
}
