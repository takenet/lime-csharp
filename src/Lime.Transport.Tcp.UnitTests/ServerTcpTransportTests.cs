using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Network.UnitTests;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.Server;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Transport.Tcp.UnitTests
{
    /// <summary>
    /// Tests for <see cref="TcpTransport"/> class using real TCP connections.
    /// </summary>
    public class ServerTcpTransportTests : ServerTransportTestsBase<TcpTransport, TcpTransport, TcpTransportListener>
    {
        public ServerTcpTransportTests()
            : base(new Uri("net.tcp://localhost:55321"))
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
