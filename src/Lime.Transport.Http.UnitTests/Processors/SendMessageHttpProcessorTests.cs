using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Serialization;
using Lime.Protocol.UnitTests;
using Lime.Transport.Http.Processors;
using Lime.Transport.Http;
using Lime.Transport.Http.Processors;
using Xunit;
using Moq;
using Shouldly;

namespace Lime.Transport.Http.UnitTests.Processors
{
    public class SendMessageHttpProcessorTests
    {
        public SendMessageHttpProcessorTests()
        {
            Principal = new Mock<IPrincipal>();
            PrincipalIdentity = new Mock<System.Security.Principal.IIdentity>();
            Principal.SetupGet(p => p.Identity).Returns(() => PrincipalIdentity.Object);
            Identity = Dummy.CreateIdentity();
            PrincipalIdentityName = Identity.ToString();
            PrincipalIdentity.SetupGet(p => p.Name).Returns(() => PrincipalIdentityName);

            Message = Dummy.CreateMessage(Dummy.CreateTextContent());
            ConsumedNotification = Dummy.CreateNotification(Event.Consumed);
            ConsumedNotification.Id = Message.Id;
            FailedNotification = Dummy.CreateNotification(Event.Failed);
            FailedNotification.Reason = Dummy.CreateReason();
            FailedNotification.Id = Message.Id;
            WaitUntilEvent = ConsumedNotification.Event;
            Content = Dummy.CreateRandomString(100);
            BodyStream = new MemoryStream(Encoding.UTF8.GetBytes(Content));
            BodyStream.Seek(0, SeekOrigin.Begin);
            SendMessageUri = new Uri("http://" + Constants.MESSAGES_PATH + ":" + Dummy.CreateRandomInt(50000) + "/" + Constants.MESSAGES_PATH);
            QueryString = new NameValueCollection();
            QueryString.Add(Constants.WAIT_UNTIL_QUERY, WaitUntilEvent.ToString().ToCamelCase());
            SendMessageHttpRequest = new HttpRequest("POST", SendMessageUri, Principal.Object, Message.Id, bodyStream: BodyStream, queryString: QueryString, contentType: MediaType.Parse(Constants.TEXT_PLAIN_HEADER_VALUE));
            SendMessageHttpRequest.Headers.Add(Constants.ENVELOPE_FROM_HEADER, Message.From.ToString());
            SendMessageHttpRequest.Headers.Add(Constants.ENVELOPE_TO_HEADER, Message.To.ToString());
            TransportSession = new Mock<ITransportSession>();
            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();

            Target = new SendMessageHttpProcessor();
        }

        public Mock<IPrincipal> Principal { get; set; }

        public Identity Identity { get; set; }

        public string PrincipalIdentityName { get; set; }

        public Mock<System.Security.Principal.IIdentity> PrincipalIdentity { get; set; }

        public Uri SendMessageUri { get; set; }


        public Message Message { get; set; }

        public Notification ConsumedNotification { get; set; }

        public Notification FailedNotification { get; set; }

        public Event WaitUntilEvent { get; set; }

        public string Content { get; set; }

        public MemoryStream BodyStream { get; set; }

        public NameValueCollection QueryString { get; set; }

        public HttpRequest SendMessageHttpRequest { get; set; }

        public Mock<ITransportSession> TransportSession { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public SendMessageHttpProcessor Target { get; set; }


        [Fact]
        public async Task ProcessAsync_SyncRequestDispatchedNotification_CallsTransportAndReturnsCreatedHttpResponse()
        {
            // Arrange
            TransportSession
                .Setup(t => t.ProcessMessageAsync(It.Is<Message>(m => m.Id == Message.Id && m.Content.ToString().Equals(Content)), WaitUntilEvent, CancellationToken))
                .ReturnsAsync(ConsumedNotification)
                .Verifiable();

            // Act
            var actual = await Target.ProcessAsync(SendMessageHttpRequest, It.IsAny<UriTemplateMatch>(), TransportSession.Object, CancellationToken);

            // Assert
            actual.CorrelatorId.ShouldBe(SendMessageHttpRequest.CorrelatorId);
            actual.StatusCode.ShouldBe(HttpStatusCode.Created);
            TransportSession.Verify();
        }

        [Fact]
        public async Task ProcessAsync_SyncRequestFailedNotification_CallsTransportAndReturnsErrorHttpResponse()
        {
            // Arrange
            TransportSession
                .Setup(t => t.ProcessMessageAsync(It.Is<Message>(m => m.Id == Message.Id && m.Content.ToString().Equals(Content)), WaitUntilEvent, CancellationToken))
                .ReturnsAsync(FailedNotification)
                .Verifiable();

            // Act
            var actual = await Target.ProcessAsync(SendMessageHttpRequest, It.IsAny<UriTemplateMatch>(), TransportSession.Object, CancellationToken);

            // Assert
            actual.CorrelatorId.ShouldBe(SendMessageHttpRequest.CorrelatorId);
            actual.StatusCode.ShouldBe(FailedNotification.Reason.ToHttpStatusCode());
            actual.StatusDescription.ShouldBe(FailedNotification.Reason.Description);
            TransportSession.Verify();
        }

        [Fact]
        public async Task ProcessAsync_AsyncRequest_CallsTransportAndReturnsAcceptedHttpResponse()
        {
            // Arrange
            SendMessageHttpRequest.QueryString.Clear();

            // Act
            var actual = await Target.ProcessAsync(SendMessageHttpRequest, It.IsAny<UriTemplateMatch>(), TransportSession.Object, CancellationToken);

            // Assert
            actual.CorrelatorId.ShouldBe(SendMessageHttpRequest.CorrelatorId);
            actual.StatusCode.ShouldBe(HttpStatusCode.Accepted);                        
        }

    }



}
