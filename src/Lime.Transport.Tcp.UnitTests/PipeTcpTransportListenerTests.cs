using Lime.Protocol.UnitTests.Network;
using System;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using NUnit.Framework;

namespace Lime.Transport.Tcp.UnitTests
{
    [TestFixture]
    public class PipeTcpTransportListenerTests : TransportListenerTestsBase
    {
        protected override ITransport CreateClientTransport() => new PipeTcpTransport(EnvelopeSerializer);

        protected override Uri CreateListenerUri() => new Uri("net.tcp://localhost:55332");

        protected override ITransportListener CreateTransportListener() 
            => new PipeTcpTransportListener(ListenerUri, null, EnvelopeSerializer, traceWriter: TraceWriter.Object);
    }
}
