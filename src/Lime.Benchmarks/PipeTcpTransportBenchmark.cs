using System;
using BenchmarkDotNet.Attributes;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using Lime.Transport.Tcp;

namespace Lime.Benchmarks
{
    [CoreJob]
    [MemoryDiagnoser]
    public class PipeTcpTransportBenchmark : TransportBenchmarkBase
    {
        protected override Uri CreateUri()
        {
            return new Uri("net.tcp://localhost:55322");
        }

        protected override ITransport CreateClientTransport()
        {
            return new PipeTcpTransport(EnvelopeSerializer, null);
        }
        
        protected override ITransportListener CreateTransportListener()
        {
            return new PipeTcpTransportListener(Uri, null, EnvelopeSerializer);
        }
    }
}
