using Lime.Protocol.UnitTests.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Lime.Transport.Tcp.UnitTests
{
    [TestFixture]
    public class TcpTransportListenerTests : TransportListenerTestsBase<TcpTransport, TcpTransportListener>
    {
        [SetUp]
        public void SetUp()
        {
            SetUp(new Uri("net.tcp://localhost:55323"));
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
