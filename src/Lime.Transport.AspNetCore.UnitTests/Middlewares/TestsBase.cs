using System.Collections.Generic;
using System.Threading;
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
        protected void SetUp(TransportEndPoint transportEndPoint)
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
            CancellationTokenSource = new CancellationTokenSource();
        }
        
        public TransportEndPoint TransportEndPoint { get; private set; }
        internal TransportListener TransportListener { get; private set; }
        public IEnvelopeSerializer EnvelopeSerializer { get; private set; }
        public LimeOptions Options { get; private set; }
        public Mock<IServiceScopeFactory> ServiceScopeFactory { get; private set; }
        public CancellationTokenSource CancellationTokenSource { get; private set; }
    }
}