using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.UnitTests;
using Lime.Transport.Http.Protocol;
using Lime.Transport.Http.Protocol.Processors;
using Lime.Transport.Http.Protocol.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Lime.Transport.Http.UnitTests.Processors
{
    [TestClass]
    public class SendEnvelopeHttpProcessorBaseTests
    {
        public Mock<IDocumentSerializer> DocumentSerializer { get; set; }

        public Mock<IPrincipal> Principal { get; set; }

        public Identity Identity { get; set; }

        public string PrincipalIdentityName { get; set; }

        public Mock<System.Security.Principal.IIdentity> PrincipalIdentity { get; set; }

        public Uri SendMessageUri { get; set; }

        public Message Envelope { get; set; }

        public string Content { get; set; }

        public MemoryStream BodyStream { get; set; }

        public HttpRequest SendMessageHttpRequest { get; set; }

        public Mock<ITransportSession> TransportSession { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public MockSendEnvelopeHttpProcessorBase Target { get; set; }

        [TestInitialize]
        public void Arrange()
        {
            DocumentSerializer = new Mock<IDocumentSerializer>();
            Principal = new Mock<IPrincipal>();
            PrincipalIdentity = new Mock<System.Security.Principal.IIdentity>();
            Principal.SetupGet(p => p.Identity).Returns(() => PrincipalIdentity.Object);
            Identity = DataUtil.CreateIdentity();
            PrincipalIdentityName = Identity.ToString();
            PrincipalIdentity.SetupGet(p => p.Name).Returns(() => PrincipalIdentityName);
            SendMessageUri = new Uri("http://" + Constants.MESSAGES_PATH + ":" + DataUtil.CreateRandomInt(50000) + "/" + Constants.MESSAGES_PATH);
            Envelope = DataUtil.CreateMessage(DataUtil.CreateTextContent());
            Content = DataUtil.CreateRandomString(100);
            BodyStream = new MemoryStream(Encoding.UTF8.GetBytes(Content));
            BodyStream.Seek(0, SeekOrigin.Begin);
            SendMessageHttpRequest = new HttpRequest("POST", SendMessageUri, Principal.Object, Envelope.Id, bodyStream: BodyStream, contentType: MediaType.Parse(Constants.TEXT_PLAIN_HEADER_VALUE));
            SendMessageHttpRequest.Headers.Add(Constants.ENVELOPE_FROM_HEADER, Envelope.From.ToString());
            SendMessageHttpRequest.Headers.Add(Constants.ENVELOPE_TO_HEADER, Envelope.To.ToString());
            TransportSession = new Mock<ITransportSession>();
            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();

            Target = new MockSendEnvelopeHttpProcessorBase(new HashSet<string>(), new UriTemplate("/"), DocumentSerializer.Object);
        }

        [TestMethod]
        public async Task ProcessAsync_ValidEnvelope_CallsTransport()
        {
            
            // Act
            var actual = await Target.ProcessAsync(SendMessageHttpRequest, It.IsAny<UriTemplateMatch>(), TransportSession.Object, CancellationToken);

            // Assert
            actual.CorrelatorId.ShouldBe(SendMessageHttpRequest.CorrelatorId);
            actual.StatusCode.ShouldBe(System.Net.HttpStatusCode.Accepted);
            TransportSession.Verify(t => t.SubmitAsync(It.Is<Message>(m => m.Id == Envelope.Id && m.From.Equals(Envelope.From) && m.To.Equals(Envelope.To)), CancellationToken), Times.Once());
            Target.Request.ShouldBe(SendMessageHttpRequest);
        }
    }

    public class MockSendEnvelopeHttpProcessorBase : SendEnvelopeHttpProcessorBase<Message>
    {
        public MockSendEnvelopeHttpProcessorBase(HashSet<string> methods, UriTemplate template, IDocumentSerializer serializer, ITraceWriter traceWriter = null)
            : base(methods, template, serializer, traceWriter)
        {
            FillEnvelopeAsyncAction = (e, r) => Task.FromResult<object>(null);
        }

        public Message Envelope { get; private set; }

        public HttpRequest Request { get; private set; }

        public Func<Envelope, HttpRequest, Task> FillEnvelopeAsyncAction { get; set; }

        protected override Task FillEnvelopeAsync(Message envelope, HttpRequest request)
        {
            Envelope = envelope;
            Request = request;

            return FillEnvelopeAsyncAction(envelope, request);
        }
    }

}
