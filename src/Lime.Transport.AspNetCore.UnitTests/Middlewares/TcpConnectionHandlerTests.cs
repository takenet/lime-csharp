using Lime.Protocol.Serialization;
using Lime.Transport.AspNetCore.Middlewares;
using Lime.Transport.AspNetCore.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    public class TcpConnectionHandlerTests
    {
        public TcpConnectionHandlerTests()
        {
            Options = new Mock<IOptions<LimeOptions>>();
            EnvelopeSerializer = new Mock<IEnvelopeSerializer>();
            ServiceScopeFactory = new Mock<IServiceScopeFactory>();
            Logger = new Mock<ILogger<TransportListener>>();
            TransportListener = new TransportListener(Options.Object, ServiceScopeFactory.Object, Logger.Object);
        }

        internal TransportListener TransportListener { get; }

        public Mock<IEnvelopeSerializer> EnvelopeSerializer { get; }

        public Mock<IOptions<LimeOptions>> Options { get; }
        
        public Mock<IServiceScopeFactory> ServiceScopeFactory { get; }
        
        internal Mock<ILogger<TransportListener>> Logger { get; }

        private TcpConnectionHandler GetTarget() =>
            new TcpConnectionHandler(TransportListener, EnvelopeSerializer.Object, Options.Object);
    }
}