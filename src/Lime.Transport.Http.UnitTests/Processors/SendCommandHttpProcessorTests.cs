using System;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Serialization;
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
    public class SendCommandHttpProcessorTests
    {        
        public Mock<IPrincipal> Principal { get; set; }

        public Identity Identity { get; set; }

        public string PrincipalIdentityName { get; set; }

        public Mock<System.Security.Principal.IIdentity> PrincipalIdentity { get; set; }

        public Uri SendCommandUri { get; set; }

        public Document Resource { get; set; }

        public Command RequestCommand { get; set; }

        public Command SuccessResponseCommand { get; set; }

        public Command ResourceSuccessResponseCommand { get; set; }

        public Command FailedResponseCommand { get; set; }

        public string RequestContent { get; set; }

        public string ResponseContent { get; set; }

        public MemoryStream BodyStream { get; set; }

        public HttpRequest SendCommandHttpRequest { get; set; }

        public Mock<ITransportSession> TransportSession { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public Mock<IDocumentSerializer> DocumentSerializer { get; set; }

        public SendCommandHttpProcessor Target { get; set; }

        [TestInitialize]
        public void Arrange()
        {
            Principal = new Mock<IPrincipal>();
            PrincipalIdentity = new Mock<System.Security.Principal.IIdentity>();
            Principal.SetupGet(p => p.Identity).Returns(() => PrincipalIdentity.Object);
            Identity = DataUtil.CreateIdentity();
            PrincipalIdentityName = Identity.ToString();
            PrincipalIdentity.SetupGet(p => p.Name).Returns(() => PrincipalIdentityName);
            Resource = DataUtil.CreatePresence();
            RequestCommand = DataUtil.CreateCommand(Resource, CommandMethod.Set, uri: new LimeUri(UriTemplates.PRESENCE));
            SuccessResponseCommand = DataUtil.CreateCommand(status: CommandStatus.Success);
            SuccessResponseCommand.Id = RequestCommand.Id;
            ResourceSuccessResponseCommand = DataUtil.CreateCommand(status: CommandStatus.Success);
            ResourceSuccessResponseCommand.Resource = DataUtil.CreatePresence();
            ResourceSuccessResponseCommand.Id = RequestCommand.Id;
            FailedResponseCommand = DataUtil.CreateCommand(status: CommandStatus.Failure);
            FailedResponseCommand.Reason = DataUtil.CreateReason();
            FailedResponseCommand.Id = RequestCommand.Id;

            RequestContent = JsonSerializer<Presence>.Serialize((Presence)Resource);
            BodyStream = new MemoryStream(Encoding.UTF8.GetBytes(RequestContent));
            ResponseContent = JsonSerializer<Presence>.Serialize((Presence)ResourceSuccessResponseCommand.Resource);

            BodyStream.Seek(0, SeekOrigin.Begin);
            SendCommandUri = new Uri("http://" + Constants.COMMANDS_PATH + ":" + DataUtil.CreateRandomInt(50000) + "/" + Constants.COMMANDS_PATH + RequestCommand.Uri.ToString());
            SendCommandHttpRequest = new HttpRequest("POST", SendCommandUri, Principal.Object, RequestCommand.Id, bodyStream: BodyStream, contentType: Resource.GetMediaType());
            SendCommandHttpRequest.Headers.Add(Constants.ENVELOPE_FROM_HEADER, RequestCommand.From.ToString());
            SendCommandHttpRequest.Headers.Add(Constants.ENVELOPE_TO_HEADER, RequestCommand.To.ToString());
            TransportSession = new Mock<ITransportSession>();
            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
            DocumentSerializer = new Mock<IDocumentSerializer>();

            Target = new SendCommandHttpProcessor(DocumentSerializer.Object);
        }


        [TestMethod]
        public async Task ProcessAsync_SuccessCommand_CallsTransportAndReturnsOKHttpResponse()
        {
            // Arrange
            TransportSession
                .Setup(t => t.ProcessCommandAsync(It.Is<Command>(c => c.Id == RequestCommand.Id && c.Uri.Equals(RequestCommand.Uri)), CancellationToken))
                .ReturnsAsync(SuccessResponseCommand)
                .Verifiable();

            // Act
            var actual = await Target.ProcessAsync(SendCommandHttpRequest, It.IsAny<UriTemplateMatch>(), TransportSession.Object, CancellationToken);

            // Assert
            TransportSession.Verify();
            actual.StatusCode.ShouldBe(HttpStatusCode.OK);
            DocumentSerializer.Verify(s => s.Serialize(It.IsAny<Document>()), Times.Never());
        }

        [TestMethod]
        public async Task ProcessAsync_ResourceSuccessCommand_CallsTransportAndReturnsOKHttpResponse()
        {
            // Arrange
            TransportSession
                .Setup(t => t.ProcessCommandAsync(It.Is<Command>(c => c.Id == RequestCommand.Id && c.Uri.Equals(RequestCommand.Uri)), CancellationToken))
                .ReturnsAsync(ResourceSuccessResponseCommand)
                .Verifiable();

            DocumentSerializer
                .Setup(s => s.Serialize(ResourceSuccessResponseCommand.Resource))
                .Returns(ResponseContent)
                .Verifiable();

            // Act
            var actual = await Target.ProcessAsync(SendCommandHttpRequest, It.IsAny<UriTemplateMatch>(), TransportSession.Object, CancellationToken);

            // Assert
            TransportSession.Verify();
            DocumentSerializer.Verify();
            actual.StatusCode.ShouldBe(HttpStatusCode.OK);

        }

        [TestMethod]
        public async Task ProcessAsync_FailuresCommand_CallsTransportAndReturnsErrorHttpResponse()
        {
            // Arrange
            TransportSession
                .Setup(t => t.ProcessCommandAsync(It.Is<Command>(c => c.Id == RequestCommand.Id && c.Uri.Equals(RequestCommand.Uri)), CancellationToken))
                .ReturnsAsync(FailedResponseCommand)
                .Verifiable();

            // Act
            var actual = await Target.ProcessAsync(SendCommandHttpRequest, It.IsAny<UriTemplateMatch>(), TransportSession.Object, CancellationToken);

            // Assert
            TransportSession.Verify();
            actual.StatusCode.ShouldBe(FailedResponseCommand.Reason.ToHttpStatusCode());
            actual.StatusDescription.ShouldBe(FailedResponseCommand.Reason.Description);
        }
    }
}
