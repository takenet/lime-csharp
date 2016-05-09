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
    public class GetEnvelopeByIdHttpProcessorBaseTests
    {
        public Mock<IEnvelopeStorage<Envelope>> EnvelopeStorage { get; set; }

        public Mock<IPrincipal> Principal { get; set; }

        public Identity Identity { get; set; }

        public string PrincipalIdentityName { get; set; }

        public Mock<System.Security.Principal.IIdentity> PrincipalIdentity { get; set; }


        public Envelope Envelope { get; set; }

        public string EnvelopeId { get; set; }
        
        public Uri GetMessageUri { get; set; }

        public HttpRequest GetMessageHttpRequest { get; set; }

        public HttpResponse GetMessageHttpResponse { get; set; }

        public UriTemplateMatch GetMessageUriTemplateMatch { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public MockGetEnvelopeByIdHttpProcessorBase Target { get; private set; }

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
            Envelope = Dummy.CreateMessage(Dummy.CreateTextContent());
            Envelope.Pp = Dummy.CreateNode();
            EnvelopeId = Envelope.Id;
            GetMessageUri = new Uri("http://" + Constants.MESSAGES_PATH + ":" + Dummy.CreateRandomInt(50000) + "/" + EnvelopeId);
            GetMessageHttpRequest = new HttpRequest("GET", GetMessageUri, Principal.Object, Guid.NewGuid());
            GetMessageUriTemplateMatch = new UriTemplateMatch();
            GetMessageUriTemplateMatch.BoundVariables.Add("id", EnvelopeId.ToString());
            GetMessageHttpResponse = new HttpResponse(GetMessageHttpRequest.CorrelatorId, System.Net.HttpStatusCode.OK);
            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();

            Target = new MockGetEnvelopeByIdHttpProcessorBase(EnvelopeStorage.Object, Constants.MESSAGES_PATH);
            Target.GetEnvelopeResponseFunc = (m, r) => GetMessageHttpResponse;                      
        }

        [Test]
        public async Task ProcessAsync_ExistingId_GetsFromStorageAndReturnsOKHttpResponse()
        {
            // Arrange
            EnvelopeStorage
                .Setup(m => m.GetEnvelopeAsync(Identity, EnvelopeId))
                .ReturnsAsync(Envelope)
                .Verifiable();

            // Act
            var actual = await Target.ProcessAsync(GetMessageHttpRequest, GetMessageUriTemplateMatch, It.IsAny<ITransportSession>(), CancellationToken);

            // Assert
            EnvelopeStorage.Verify();
            Target.Envelope.ShouldNotBe(null);
            Target.Envelope.ShouldBe(Envelope);
            Target.Request.ShouldNotBe(null);
            Target.Request.ShouldBe(GetMessageHttpRequest);
            actual.ShouldBe(GetMessageHttpResponse);
            actual.Headers[Constants.ENVELOPE_FROM_HEADER].ShouldBe(Envelope.From.ToString());
            actual.Headers[Constants.ENVELOPE_TO_HEADER].ShouldBe(Envelope.To.ToString());
            actual.Headers[Constants.ENVELOPE_PP_HEADER].ShouldBe(Envelope.Pp.ToString());
        }

        [Test]
        public async Task ProcessAsync_NonExistingId_ReturnsNotFoundHttpResponse()
        {
            // Arrange
            EnvelopeStorage
                .Setup(m => m.GetEnvelopeAsync(Identity, EnvelopeId))
                .ReturnsAsync(null)
                .Verifiable();

            // Act
            var actual = await Target.ProcessAsync(GetMessageHttpRequest, GetMessageUriTemplateMatch, It.IsAny<ITransportSession>(), CancellationToken);

            // Assert
            EnvelopeStorage.Verify();
            actual.CorrelatorId.ShouldBe(GetMessageHttpRequest.CorrelatorId);
            actual.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task ProcessAsync_RequestUriWithoutId_RetunsBadRequestHttpResponse()
        {
            // Arrange
            GetMessageUriTemplateMatch.BoundVariables.Clear();

            // Act
            var actual = await Target.ProcessAsync(GetMessageHttpRequest, GetMessageUriTemplateMatch, It.IsAny<ITransportSession>(), CancellationToken.None);

            // Assert
            actual.CorrelatorId.ShouldBe(GetMessageHttpRequest.CorrelatorId);
            actual.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            EnvelopeStorage.Verify();
        }

        [Test]
        public async Task ProcessAsync_InvalidPrincipalNameFormat_RetunsBadRequestHttpResponse()
        {
            // Arrange
            PrincipalIdentityName = string.Empty;

            // Act
            var actual = await Target.ProcessAsync(GetMessageHttpRequest, GetMessageUriTemplateMatch, It.IsAny<ITransportSession>(), CancellationToken.None);

            // Assert
            actual.CorrelatorId.ShouldBe(GetMessageHttpRequest.CorrelatorId);
            actual.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            EnvelopeStorage.Verify();
        }
    }

    public class MockGetEnvelopeByIdHttpProcessorBase : GetEnvelopeByIdHttpProcessorBase<Envelope>
    {
        public MockGetEnvelopeByIdHttpProcessorBase(IEnvelopeStorage<Envelope> envelopeStorage, string path)
            : base(envelopeStorage, path)
        {
            GetEnvelopeResponseFunc = (e, r) => null;
        }

        public Envelope Envelope { get; private set; }

        public HttpRequest Request { get; private set; }

        public Func<Envelope, HttpRequest, HttpResponse> GetEnvelopeResponseFunc { get; set; }


        protected override HttpResponse GetEnvelopeResponse(Envelope envelope, HttpRequest request)
        {
            Envelope = envelope;
            Request = request;

            return GetEnvelopeResponseFunc(envelope, request);            
        }
    }
    
}
