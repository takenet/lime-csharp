using System.Threading.Tasks;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Transport.AspNetCore.Middlewares;
using Lime.Transport.AspNetCore.Transport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    [TestFixture]
    public class WebSocketMiddlewareTests
    {
        private WebSocketMiddleware GetTarget() => new WebSocketMiddleware(
            Next, EnvelopeSerializer, Listener, Options);

        public IOptions<LimeOptions> Options { get; set; }

        internal TransportListener Listener { get; set; }

        public IEnvelopeSerializer EnvelopeSerializer { get; set; }

        private Task Next(HttpContext context)
        {
            throw new System.NotImplementedException();
        }

        [Test]
        public async Task Invoke_WebSocketRequest_ShouldOpenTransport()
        {
            
            
        }
        
    }
}