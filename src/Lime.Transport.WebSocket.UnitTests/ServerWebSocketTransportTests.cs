using System.Threading.Tasks;
using Lime.Protocol.Serialization;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Transport.WebSocket.UnitTests
{
    [TestFixture]
    public class ServerWebSocketTransportTests
    {
        public ServerWebSocketTransport Target { get; set; }

        [SetUp]
        public void SetUp()
        {
            
        }

        [Test]
        public async Task SendAsync_SessionEnvelope_ShouldSucceed()
        {
            
        }
    }
}