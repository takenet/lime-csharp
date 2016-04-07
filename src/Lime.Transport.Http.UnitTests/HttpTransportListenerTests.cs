using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.UnitTests;
using Lime.Transport.Http;
using NUnit.Framework;
using Moq;
using Shouldly;
using TransportEventArgs = Lime.Transport.Http.TransportEventArgs;

namespace Lime.Transport.Http.UnitTests
{
    [TestFixture]
    public class HttpTransportListenerTests
    {
        #region Public properties

        public string Host { get; private set; }

        public int Port { get; private set; }

        public Mock<IHttpServer> HttpServer { get; private set; }

        public Task<HttpResponse> HttpServerResponse { get; private set; }

        public BufferBlock<HttpRequest> HttpServerRequestBuffer { get; private set; }

        public Mock<IHttpTransportProvider> HttpTransportProvider { get; private set; }
        public Mock<IPrincipal> Principal { get; private set; }

        public Mock<ITransport> Transport { get; private set; }

        public DateTimeOffset SessionExpiration { get; private set; }

        public Mock<ITransportSession> EmulatedTransport { get; private set; }

        public Mock<IHttpProcessor> Processor1 { get; private set; }

        public HashSet<string> Processor1Methods { get; private set; }

        public UriTemplate Processor1Template { get; private set; }

        public HttpRequest Processor1HttpRequest { get; private set; }

        public Mock<IHttpProcessor> Processor2 { get; private set; }

        public HashSet<string> Processor2Methods { get; private set; }

        public UriTemplate Processor2Template { get; private set; }

        public HttpTransportListener Target { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public string User { get; private set; }

        public string Password { get; private set; }

        public string MessagesUrl { get; private set; }

        public TimeSpan SendTimeout { get; private set; }

        public string SessionId { get; private set; }

        public Node ClientNode { get; private set; }

        public Node ServerNode { get; private set; }

        public Reason Reason { get; private set; }

        public Message SentMessage { get; private set; }

        public Notification Notification { get; set; }

        #endregion

        [SetUp]
        public void Arrange()
        {
            Host = "localhost";
            Port = 8080 + Dummy.CreateRandomInt(10000);
            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
            ClientNode = Dummy.CreateNode();
            User = ClientNode.ToIdentity().ToString();
            Password = Dummy.CreateRandomString(20);
            MessagesUrl = "http://" + Host + ":" + Port + "/messages";

            SessionId = Guid.NewGuid().ToString().ToString();
            ServerNode = Dummy.CreateNode();
            SendTimeout = TimeSpan.FromSeconds(5);
            Reason = Dummy.CreateReason();

            SentMessage = new Message()
            {
                To = Dummy.CreateNode(),
                Content = Dummy.CreateTextContent()
            };

            HttpServer = new Mock<IHttpServer>();
            HttpServerRequestBuffer = new BufferBlock<HttpRequest>();

            var tcs = new TaskCompletionSource<HttpResponse>();
            HttpServer
                .Setup(s => s.SubmitResponseAsync(It.IsAny<HttpResponse>()))
                .Callback((HttpResponse r) => tcs.SetResult(r));
            HttpServerResponse = tcs.Task;

            HttpServer
                .Setup(s => s.AcceptRequestAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) => HttpServerRequestBuffer.ReceiveAsync(cancellationToken));

            Transport = new Mock<ITransport>();            
            EmulatedTransport = new Mock<ITransportSession>();
            SessionExpiration = DateTimeOffset.Parse("Mon, 15 Jun 2009 20:45:30 GMT");
            EmulatedTransport.SetupGet(t => t.Expiration).Returns(() => SessionExpiration);
            Principal = new Mock<IPrincipal>();
            HttpTransportProvider = new Mock<IHttpTransportProvider>();
            HttpTransportProvider
                .Setup(h => h.GetTransport(Principal.Object, It.IsAny<bool>()))
                .Returns(() => EmulatedTransport.Object);

            Processor1Methods = new HashSet<string> { "GET" };
            Processor1Template = new UriTemplate("/" + Dummy.CreateRandomString(10));
            Processor1 = new Mock<IHttpProcessor>();
            Processor1.SetupGet(p => p.Methods).Returns(Processor1Methods);
            Processor1.SetupGet(p => p.Template).Returns(Processor1Template);

            var uri = new Uri("http://" + Host + ":" + Port + Processor1Template);
            Processor1HttpRequest = new HttpRequest("GET", uri, Principal.Object);

            Processor2Methods = new HashSet<string> { "POST", "DELETE", "GET" };
            Processor2Template = new UriTemplate("/" + Dummy.CreateRandomString(10) + "/{id}");
            Processor2 = new Mock<IHttpProcessor>();
            Processor2.SetupGet(p => p.Methods).Returns(Processor2Methods);
            Processor2.SetupGet(p => p.Template).Returns(Processor2Template);

            Target = new HttpTransportListener(
                Port,
                Host,
                httpServer: HttpServer.Object,
                httpTransportProvider: HttpTransportProvider.Object,
                processors: new[] { Processor1.Object, Processor2.Object });

        }

        [TearDown]
        public void Dispose()
        {
            if (Target != null)
            {
                Target.Dispose();
            }
        }

        [Test]
        public void ListenerUris_ValidHostAndPort_GetsRegisteredUris()
        {
            // Act
            var listenerUris = Target.ListenerUris;

            // Assert
            listenerUris.ShouldNotBe(null);
            listenerUris.Length.ShouldBe(3);
            listenerUris.ShouldAllBe(u => u.Host == Host && u.Port == Port);

        }

        [Test]
        public async Task StartAsync_ValidHostAndPort_ServerStarted()
        {
            // Act
            await Target.StartAsync();

            // Assert
            HttpServer.Verify(s => s.Start(), Times.Once());
        }

        [Test]
        public async Task StartAsync_CallTwice_ThrowsInvalidOperationException()
        {
            // Act
            await Target.StartAsync();
            Should.ThrowAsync<InvalidOperationException>(async () => await Target.StartAsync());
        }

        [Test]
        public async Task AcceptTransportAsync_NewRequest_RetunsTransport()
        {
            // Act
            await Target.StartAsync();
            HttpTransportProvider.Raise(h => h.TransportCreated += null, new TransportEventArgs(Transport.Object));
            var transport = await Target.AcceptTransportAsync(CancellationToken);

            // Assert
            transport.ShouldBe(Transport.Object);
        }

        [Test]
        public async Task AcceptTransportAsync_ListenerNotStarted_ThrowsInvalidOperationException()
        {
            // Act
            var transport = await Target.AcceptTransportAsync(CancellationToken).ShouldThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task StopAsync_ActiveListener_StopsListening()
        {
            // Act
            await Target.StartAsync();

            await Target.StopAsync();

            // Assert
            HttpServer.Verify(s => s.Stop(), Times.Once());
        }

        [Test]
        public async Task StopAsync_ListenerNotStarted_ThrowsInvalidOperationException()
        {
            // Act
            await Target.StopAsync().ShouldThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task ProcessAsync_ValidUrlAuthenticatedUser_CallProcessorAndKeepSession()
        {
            // Arrange            
            var session = new Session()
            {
                Id = Guid.NewGuid().ToString().ToString(),
                State = SessionState.Established
            };

            EmulatedTransport
                .Setup(e => e.GetSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var processorResponse = new HttpResponse(Processor1HttpRequest.CorrelatorId, HttpStatusCode.OK);

            Processor1
                .Setup(p => p.ProcessAsync(Processor1HttpRequest, It.IsAny<UriTemplateMatch>(), It.IsAny<ITransportSession>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(processorResponse)
                .Verifiable();

            // Act
            await Target.StartAsync();
            HttpServerRequestBuffer.Post(Processor1HttpRequest);

            // Assert
            var actualResponse = await HttpServerResponse;
            Processor1.Verify();
            actualResponse.ShouldBe(processorResponse);
            EmulatedTransport.Verify(t => t.FinishAsync(It.IsAny<CancellationToken>()), Times.Never());
            actualResponse.Headers.Get(Constants.SESSION_EXPIRATION_HEADER).ShouldNotBe(null);
            var actualSessionExpiration = DateTimeOffset.Parse(actualResponse.Headers.Get(Constants.SESSION_EXPIRATION_HEADER));
            actualSessionExpiration.ShouldBe(SessionExpiration);
            HttpTransportProvider.Verify(h => h.GetTransport(Principal.Object, true), Times.Once());
        }

        [Test]
        public async Task ProcessAsync_InvalidSessionAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var session = new Session()
            {
                Id = Guid.NewGuid().ToString().ToString(),
                State = SessionState.Failed,
                Reason = new Reason()
                {
                    Code = ReasonCodes.SESSION_AUTHENTICATION_FAILED,
                    Description = Dummy.CreateRandomString(50)
                }
            };

            EmulatedTransport
                .Setup(e => e.GetSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            // Act
            await Target.StartAsync();
            HttpServerRequestBuffer.Post(Processor1HttpRequest);

            // Assert
            var actualResponse = await HttpServerResponse;
            actualResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            actualResponse.StatusDescription.ShouldBe(session.Reason.Description);
        }

        [Test]
        public async Task ProcessAsync_FailedSessionEmptyReason_ReturnsServiceUnavailable()
        {
            // Arrange
            var session = new Session()
            {
                Id = Guid.NewGuid().ToString(),
                State = SessionState.Failed
            };

            EmulatedTransport
                .Setup(e => e.GetSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            // Act
            await Target.StartAsync();
            HttpServerRequestBuffer.Post(Processor1HttpRequest);

            // Assert
            var actualResponse = await HttpServerResponse;
            actualResponse.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        }

        [Test]
        public async Task ProcessAsync_RequestTimedOut_ReturnsTimeout()
        {
            // Arrange
            EmulatedTransport
                .Setup(e => e.GetSessionAsync(It.IsAny<CancellationToken>()))
                .Throws<OperationCanceledException>();

            // Act
            await Target.StartAsync();
            HttpServerRequestBuffer.Post(Processor1HttpRequest);

            // Assert
            var actualResponse = await HttpServerResponse;
            actualResponse.StatusCode.ShouldBe(HttpStatusCode.RequestTimeout);
        }

        [Test]
        public async Task ProcessAsync_InvalidUrl_ReturnsNotFound()
        {
            // Arrange
            var invalidHttpRequest = new HttpRequest(
                "GET", 
                new Uri("http://" + Host + ":" + Port + "/" + Dummy.CreateRandomString(10)), 
                Principal.Object);

            // Act
            await Target.StartAsync();
            HttpServerRequestBuffer.Post(invalidHttpRequest);

            // Assert
            var actualResponse = await HttpServerResponse;
            actualResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task ProcessAsync_ProcessorRaisesException_ReturnsInternalServerError()
        {
            // Arrange            
            var session = new Session()
            {
                Id = Guid.NewGuid().ToString(),
                State = SessionState.Established
            };

            EmulatedTransport
                .Setup(e => e.GetSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var exceptionMessage = Dummy.CreateRandomString(100);

            Processor1
                .Setup(p => p.ProcessAsync(Processor1HttpRequest, It.IsAny<UriTemplateMatch>(), It.IsAny<ITransportSession>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception(exceptionMessage))
                .Verifiable();


            // Act
            await Target.StartAsync();
            HttpServerRequestBuffer.Post(Processor1HttpRequest);

            // Assert
            var actualResponse = await HttpServerResponse;
            actualResponse.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
            actualResponse.BodyStream.ShouldNotBe(null);

            var reader = new StreamReader(actualResponse.BodyStream);
            var body = reader.ReadToEnd();
            body.ShouldContain(exceptionMessage);

            Processor1.Verify();
        }

        [Test]
        public async Task ProcessAsync_SessionCloseHeader_FinishTransport()
        {
            // Arrange            
            var session = new Session()
            {
                Id = Guid.NewGuid().ToString(),
                State = SessionState.Established
            };

            EmulatedTransport
                .Setup(e => e.GetSessionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            Processor1HttpRequest.Headers.Add(Constants.SESSION_HEADER, Constants.CLOSE_HEADER_VALUE);

            var processorResponse = new HttpResponse(Processor1HttpRequest.CorrelatorId, HttpStatusCode.OK);

            Processor1
                .Setup(p => p.ProcessAsync(Processor1HttpRequest, It.IsAny<UriTemplateMatch>(), It.IsAny<ITransportSession>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(processorResponse)
                .Verifiable();

            // Act
            await Target.StartAsync();
            HttpServerRequestBuffer.Post(Processor1HttpRequest);

            // Assert
            var actualResponse = await HttpServerResponse;

            Processor1.Verify();
            actualResponse.ShouldBe(processorResponse);
            EmulatedTransport.Verify(t => t.FinishAsync(It.IsAny<CancellationToken>()), Times.Once());
            HttpTransportProvider.Verify(h => h.GetTransport(Principal.Object, false), Times.Once());
        }

    }
}