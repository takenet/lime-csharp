using System.Net;
using System.Threading.Tasks;
using Lime.Transport.AspNetCore.Middlewares;
using NUnit.Framework;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    [TestFixture]
    public class WebSocketMiddlewareTests : TestsBase
    {
        public WebSocketMiddlewareTests()
        : base(new TransportEndPoint()
        {
            Transport = TransportType.WebSocket,
            EndPoint = new IPEndPoint(IPAddress.Any, 443)
        })
        {
            RequestDelegateExecutor = new RequestDelegateExecutor();
        }

        public RequestDelegateExecutor RequestDelegateExecutor { get;  }

        private WebSocketMiddleware GetTarget() => new WebSocketMiddleware(
            RequestDelegateExecutor.Next, 
            EnvelopeSerializer, 
            TransportListener, 
            Microsoft.Extensions.Options.Options.Create(Options));

        [Test]
        public async Task Invoke_WebSocketRequest_ShouldOpenTransport()
        {

        }
    }
}