using Lime.Protocol.UnitTests.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using NUnit.Framework;

namespace Lime.Transport.Tcp.UnitTests
{
    [TestFixture]
    public class TcpTransportListenerTests : TransportListenerTestsBase
    {
        protected override ITransport CreateClientTransport() => new TcpTransport(EnvelopeSerializer);

        protected override Uri CreateListenerUri() => new Uri("net.tcp://localhost:55323");

        protected override ITransportListener CreateTransportListener() 
            => new TcpTransportListener(ListenerUri, null, EnvelopeSerializer, traceWriter: TraceWriter.Object);
    }
}
