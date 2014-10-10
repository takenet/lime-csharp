using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using Lime.Protocol.UnitTests;
using System.Net.Sockets;
using Shouldly;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using System.Net.Http.Headers;
using System.Text;

namespace Lime.Protocol.Http.UnitTests
{
    [TestClass]
    public class HttpServerTests
    {
        public string Path { get; set; }

        public int Port { get; set; }
        
        public string[] Prefixes { get; set; }

        public AuthenticationSchemes AuthenticationSchemes { get; set; }

        public TcpClient TcpClient { get; set; }

        public Identity Identity { get; set; }

        public string Password { get; set; }

        public NetworkCredential Credential { get; set; }

        public HttpClientHandler HttpClientHandler { get; set; }        

        public HttpClient HttpClient { get; set; }


        public Guid EnvelopeId { get; set; }

        public string QueryStringValue1 { get; set; }

        public int QueryStringValue2 { get; set; }

        public Uri RequestUri { get; set; }

        public HttpRequestMessage GetHttpRequestMessage { get; set; }

        public MediaType RequestBodyMediaType { get; set; }

        public string RequestBody { get; set; }

        public HttpRequestMessage PostHttpRequestMessage { get; set; }


        public WebHeaderCollection HttpResponseHeaders { get; set; }

        public MediaType ResponseBodyMediaType { get; set; }

        public string ResponseBody { get; set; }

        public HttpResponse HttpResponse { get; set; }



        public CancellationToken CancellationToken { get; set; }

        public Lazy<HttpServer> Target { get; set; }



        [TestInitialize]
        public void Arrange()
        {
            Path = "/" + DataUtil.CreateRandomString(15);
            Port = 50000 + DataUtil.CreateRandomInt(1000);            
            Prefixes = new[]
            {
                "http://*:" + Port  + Path + "/"
            };
            AuthenticationSchemes = AuthenticationSchemes.Basic;
            TcpClient = new TcpClient();
            Identity = DataUtil.CreateIdentity();
            Password = DataUtil.CreateRandomString(20);
            Credential = new NetworkCredential(Identity.ToString(), Password);
            HttpClientHandler = new HttpClientHandler() { Credentials = Credential };
            HttpClient = new HttpClient(HttpClientHandler);
            EnvelopeId = Guid.NewGuid();
            QueryStringValue1 = DataUtil.CreateRandomString(10);
            QueryStringValue2 = DataUtil.CreateRandomInt(100);
            RequestUri = new System.Uri("http://localhost:" + Port + Path + "/?value1=" + QueryStringValue1 + "&value2=" + QueryStringValue2);
            GetHttpRequestMessage = new HttpRequestMessage(HttpMethod.Get, RequestUri);
            GetHttpRequestMessage.Headers.Add(Constants.ENVELOPE_ID_HEADER, EnvelopeId.ToString());

            RequestBodyMediaType = DataUtil.CreateJsonMediaType();
            RequestBody = DataUtil.CreateMessageJson();
            PostHttpRequestMessage = new HttpRequestMessage(HttpMethod.Post, RequestUri);
            PostHttpRequestMessage.Content = new StringContent(RequestBody, Encoding.UTF8, RequestBodyMediaType.ToString());

            HttpResponseHeaders = new WebHeaderCollection();
            HttpResponseHeaders.Add(DataUtil.CreateRandomString(10), DataUtil.CreateRandomString(10));
            HttpResponseHeaders.Add(DataUtil.CreateRandomString(10), DataUtil.CreateRandomString(10));
            HttpResponseHeaders.Add(DataUtil.CreateRandomString(10), DataUtil.CreateRandomString(10));
            ResponseBodyMediaType = DataUtil.CreateJsonMediaType();
            ResponseBody = DataUtil.CreateMessageJson();
            HttpResponse = new Http.HttpResponse(EnvelopeId, HttpStatusCode.OK, DataUtil.CreateRandomString(50), HttpResponseHeaders, ResponseBodyMediaType, ResponseBody);

            CancellationToken = TimeSpan.FromSeconds(5).ToCancellationToken();
            Target = new Lazy<HttpServer>(() => new HttpServer(Prefixes, AuthenticationSchemes));
        }

        [TestMethod]
        public void Start_LocalUri_ListensToAddress()
        {
            // Act
            Target.Value.Start();

            // Assert
            TcpClient.Connect("localhost", Port);            
        }

        [TestMethod]
        public void Stop_LocalUri_StopsListening()
        {
            // Arrange
            Target.Value.Start();
            var tcpClient = new TcpClient();
            tcpClient.Connect("localhost", Port);
            tcpClient.Close();

            // Act
            Target.Value.Stop();
            try
            {
                TcpClient.Connect("localhost", Port);
            }
            catch (SocketException ex)
            {
                ex.SocketErrorCode.ShouldBe(SocketError.ConnectionRefused);
            }            
        }

        [TestMethod]
        public async Task AcceptRequestAsync_GetRequest_ReturnsHttpRequest()
        {
            // Arrange
            Target.Value.Start();
            
            // Act
            var acceptRequestTask = Target.Value.AcceptRequestAsync(CancellationToken);
            var httpRequestTask = HttpClient.SendAsync(GetHttpRequestMessage, CancellationToken);
            var actual = await acceptRequestTask;
            
            // Assert
            actual.Method.ShouldBe("GET");
            var httpListenerBasicIdentity = actual.User.Identity.ShouldBeOfType<HttpListenerBasicIdentity>();
            httpListenerBasicIdentity.Name.ShouldBe(Identity.ToString());
            httpListenerBasicIdentity.Password.ShouldBe(Password);
            actual.Uri.ShouldBe(RequestUri);
            actual.CorrelatorId.ShouldBe(EnvelopeId);
            actual.QueryString.ShouldNotBe(null);
            actual.QueryString.Get("value1").ShouldBe(QueryStringValue1);
            actual.QueryString.Get("value2").ShouldBe(QueryStringValue2.ToString());
        }

        [TestMethod]
        public async Task AcceptRequestAsync_PostRequestNoId_ReturnsHttpRequestWithBody()
        {
            // Arrange
            Target.Value.Start();

            // Act
            var acceptRequestTask = Target.Value.AcceptRequestAsync(CancellationToken);
            var httpRequestTask = HttpClient.SendAsync(PostHttpRequestMessage, CancellationToken);
            var actual = await acceptRequestTask;

            // Assert
            actual.Method.ShouldBe("POST");
            var httpListenerBasicIdentity = actual.User.Identity.ShouldBeOfType<HttpListenerBasicIdentity>();
            httpListenerBasicIdentity.Name.ShouldBe(Identity.ToString());
            httpListenerBasicIdentity.Password.ShouldBe(Password);
            actual.Uri.ShouldBe(RequestUri);
            actual.CorrelatorId.ShouldNotBe(Guid.Empty);
            actual.QueryString.ShouldNotBe(null);
            actual.QueryString.Get("value1").ShouldBe(QueryStringValue1);
            actual.QueryString.Get("value2").ShouldBe(QueryStringValue2.ToString());
            actual.ContentType.ShouldBe(RequestBodyMediaType);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException), AllowDerivedTypes = true)]
        public async Task AcceptRequestAsync_CancelRequest_ThrowsTaskCanceledException()
        {
            // Arrange
            Target.Value.Start();
            var cts = new CancellationTokenSource();

            // Act
            var acceptRequestTask = Target.Value.AcceptRequestAsync(cts.Token);
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));
            var actual = await acceptRequestTask;
        }

        [TestMethod]
        public async Task SubmitResponseAsync_ExistingCorrelationId_CompletesRequest()
        {
            // Arrange
            Target.Value.Start();
            var acceptRequestTask = Target.Value.AcceptRequestAsync(CancellationToken);
            var httpRequestTask = HttpClient.SendAsync(GetHttpRequestMessage, CancellationToken);
            var httpRequest = await acceptRequestTask;
            
            // Act
            await Target.Value.SubmitResponseAsync(HttpResponse);
            var httpResponseMessage = await httpRequestTask;
            
            // Assert
            httpResponseMessage.StatusCode.ShouldBe(HttpResponse.StatusCode);
            httpResponseMessage.ReasonPhrase.ShouldBe(HttpResponse.StatusDescription);
            foreach (var headerName in HttpResponseHeaders.AllKeys)
            {
                var headerValue = HttpResponseHeaders.Get(headerName);

                if (headerName.StartsWith("Content"))
                {
                    httpResponseMessage.Content.Headers.GetValues(headerName).ShouldNotBe(null);
                    httpResponseMessage.Content.Headers.GetValues(headerName).First().ShouldBe(headerValue);
                }
                else
                {
                    httpResponseMessage.Headers.GetValues(headerName).ShouldNotBe(null);
                    httpResponseMessage.Headers.GetValues(headerName).First().ShouldBe(headerValue);
                }                
            }
            var responseBody = await httpResponseMessage.Content.ReadAsStringAsync();
            responseBody.ShouldBe(ResponseBody);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task SubmitResponseAsync_InvalidCorrelationId_ThrowsArgumentException()
        {
            // Act
            await Target.Value.SubmitResponseAsync(HttpResponse);
        }
    }
}
