using System;
using System.Collections.Generic;
using System.Threading;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Transport.AspNetCore.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lime.Transport.AspNetCore.UnitTests
{
    public abstract class TestsBase
    {
        protected void SetUp(TransportEndPoint transportEndPoint = null)
        {
            TransportEndPoint = transportEndPoint ?? new TransportEndPoint();
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
                new ChannelProvider(),
                new Logger<TransportListener>(new LoggerFactory()));
            CancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            Scope = new Mock<IServiceScope>();
            ServiceScopeFactory
                .Setup(s => s.CreateScope())
                .Returns(Scope.Object);
            ServiceProvider = new Mock<IServiceProvider>();
            Scope
                .SetupGet(s => s.ServiceProvider)
                .Returns(ServiceProvider.Object);
            ServiceProvider
                .Setup(s => s.GetService(typeof(ChannelContextProvider)))
                .Returns(() =>
                {
                    var provider = new ChannelContextProvider();
                    ChannelContextProviders.Add(provider);
                    return provider;
                });
            SenderChannel = new Mock<ISenderChannel>();
            ChannelContextProviders = new List<ChannelContextProvider>();
        }

        protected void TearDown()
        {
            CancellationTokenSource.Dispose();
        }

        public TransportEndPoint TransportEndPoint { get; private set; }
        internal TransportListener TransportListener { get; private set; }
        public IEnvelopeSerializer EnvelopeSerializer { get; private set; }
        public LimeOptions Options { get; private set; }
        public Mock<IServiceScopeFactory> ServiceScopeFactory { get; private set; }
        public CancellationTokenSource CancellationTokenSource { get; private set; }
        public Mock<IServiceScope> Scope { get; private set; }
        public Mock<IServiceProvider> ServiceProvider { get; private set; }
        public Mock<ISenderChannel> SenderChannel { get; private set; }
        internal List<ChannelContextProvider> ChannelContextProviders { get; private set; }
    }
}