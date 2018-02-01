using Lime.Protocol.UnitTests.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lime.Transport.Tcp.UnitTests
{
    [TestClass]
    public class TcpTransportListenerTests : TransportListenerTestsBase<TcpTransport, TcpTransportListener>
    {
        public TcpTransportListenerTests()
            : base(new Uri("net.tcp://localhost:55323"))
        {
        }

        protected override TcpTransport CreateClientTransport()
        {
            return new TcpTransport(EnvelopeSerializer);
        }

        protected override TcpTransportListener CreateTransportListener()
        {
            return new TcpTransportListener(ListenerUri, null, EnvelopeSerializer, traceWriter: TraceWriter.Object);
        }
    }
}
