using BenchmarkDotNet.Attributes;
using Lime.Messaging;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests;
using Lime.Transport.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Benchmarks
{
    [CoreJob]
    [MemoryDiagnoser]
    public class PipeWebSocketTransportBenchmark : TransportBenchmarkBase
    {
        protected override Uri CreateUri()
        {
            return new Uri("ws://localhost:8082");
        }

        protected override ITransport CreateClientTransport()
        {
            return new PipeClientWebSocketTransport(EnvelopeSerializer);
        }

        protected override ITransportListener CreateTransportListener()
        {
            return new PipeWebSocketTransportListener(new[] { Uri }, EnvelopeSerializer);
        }
    }
}
