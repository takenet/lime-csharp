using System.IO;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    public abstract class HttpMiddlewareTestsBase : TestsBase
    {
        protected void SetUp(TransportEndPoint transportEndPoint)
        {
            base.SetUp(transportEndPoint);
            
            FakeRequestDelegate = new FakeRequestDelegate();
            HttpContext = new Mock<HttpContext>();
            HttpRequest = new Mock<HttpRequest>();
            HttpResponse = new Mock<HttpResponse>();
            ConnectionInfo = new Mock<ConnectionInfo>();
            Headers = new HeaderDictionary();
            RequestBody = new MemoryStream();
            ResponseBody = new MemoryStream();
            ConnectionInfo
                .SetupGet(c => c.LocalPort)
                .Returns(TransportEndPoint.EndPoint.Port);
            HttpContext
                .SetupGet(c => c.Connection)
                .Returns(ConnectionInfo.Object);
            HttpContext
                .SetupGet(c => c.RequestAborted)
                .Returns(CancellationTokenSource.Token);
            HttpContext
                .SetupGet(c => c.Response)
                .Returns(HttpResponse.Object);
            HttpRequest
                .SetupGet(r => r.Headers)
                .Returns(Headers);
            HttpRequest
                .SetupGet(r => r.Body)
                .Returns(RequestBody);
            HttpRequest
                .SetupGet(r => r.Method)
                .Returns("POST");
            HttpRequest
                .SetupGet(r => r.ContentType)
                .Returns("application/json");
            HttpRequest
                .SetupGet(r => r.Path)
                .Returns("/messages");
            HttpContext
                .SetupGet(c => c.Request)
                .Returns(HttpRequest.Object);
            HttpResponse
                .SetupGet(r => r.Body)
                .Returns(ResponseBody);
        }
        
        public FakeRequestDelegate FakeRequestDelegate { get; private set; }
        public Mock<ConnectionInfo> ConnectionInfo { get; private set; }
        public Mock<HttpContext> HttpContext { get; private set; }
        public Mock<HttpRequest> HttpRequest { get; private set; }
        public Mock<HttpResponse> HttpResponse { get; private set; }
        public HeaderDictionary Headers { get; private set; }
        public MemoryStream RequestBody { get; private set; }
        public MemoryStream ResponseBody { get; private set; }
    }
}