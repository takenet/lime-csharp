using Microsoft.AspNetCore.Http;
using Moq;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    public abstract class HttpMiddlewareTestsBase : TestsBase
    {
        protected void SetUp(TransportEndPoint transportEndPoint)
        {
            base.SetUp(transportEndPoint);
            
            RequestDelegateExecutor = new RequestDelegateExecutor();
            HttpContext = new Mock<HttpContext>();
            HttpResponse = new Mock<HttpResponse>();
            ConnectionInfo = new Mock<ConnectionInfo>();
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
        }
        
        public RequestDelegateExecutor RequestDelegateExecutor { get; private set; }
        public Mock<ConnectionInfo> ConnectionInfo { get; private set; }
        public Mock<HttpContext> HttpContext { get; private set; }
        public Mock<HttpResponse> HttpResponse { get; private set; }
        
    }
}