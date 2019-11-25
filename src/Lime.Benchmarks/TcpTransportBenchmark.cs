using System;
using BenchmarkDotNet.Attributes;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using Lime.Transport.Tcp;

namespace Lime.Benchmarks
{
    [CoreJob]
    [MemoryDiagnoser]
    public class TcpTransportBenchmark : TransportBenchmarkBase
    {
        protected override Uri CreateUri()
        {
            return new Uri("net.tcp://localhost:55321");
        }

        protected override ITransport CreateClientTransport()
        {
            return new TcpTransport(EnvelopeSerializer, null);
        }

        protected override ITransportListener CreateTransportListener()
        {
            return new TcpTransportListener(Uri, null, EnvelopeSerializer);
        }
    }
}
