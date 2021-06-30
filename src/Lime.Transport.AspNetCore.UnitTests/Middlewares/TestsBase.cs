using System.Collections.Generic;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Transport.AspNetCore.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    public abstract class TestsBase
    {
        protected TestsBase(TransportEndPoint transportEndPoint)
        {
            TransportEndPoint = transportEndPoint;
            Options = new LimeOptions()
            {
                EndPoints = new List<TransportEndPoint>()
                {
                    TransportEndPoint
                }
            };
            EnvelopeSerializer = new EnvelopeSerializer(new DocumentTypeResolver());
            ServiceScopeFactory = new Mock<IServiceScopeFactory>();
            TransportListener = new TransportListener(
                Microsoft.Extensions.Options.Options.Create(Options), 
                ServiceScopeFactory.Object, 
                new Logger<TransportListener>(new LoggerFactory()));
        }
        
        public TransportEndPoint TransportEndPoint { get; }
        internal TransportListener TransportListener { get; }
        public IEnvelopeSerializer EnvelopeSerializer { get; }
        public LimeOptions Options { get; }
        public Mock<IServiceScopeFactory> ServiceScopeFactory { get; }
    }
}