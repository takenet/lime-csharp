using System;
using Lime.Protocol.Serialization;
using Lime.Transport.Tcp;
using NUnit.Framework;
using Lime.Protocol.Network;
using Lime.Protocol.Server;

namespace Lime.Protocol.LoadTests.Tcp
{
    [TestFixture]
    public class TcpTransportLoadTests : TransportLoadTestsBase
    {
        protected override Uri CreateUri() => new Uri("net.tcp://localhost:55321");

        protected override ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer)
        {
            return new TcpTransportListener(uri, null, envelopeSerializer, maxBufferSize: TcpTransport.DEFAULT_MAX_BUFFER_SIZE * 2);
        }

        protected override ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer)
        {
            return new TcpTransport(envelopeSerializer, maxBufferSize: TcpTransport.DEFAULT_MAX_BUFFER_SIZE * 2);
        }
    }
}
