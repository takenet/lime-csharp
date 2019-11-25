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
    public class WebSocketTransportBenchmark : TransportBenchmarkBase
    {
        protected override Uri CreateUri()
        {
            return new Uri("ws://localhost:8081");
        }

        protected override ITransport CreateClientTransport()
        {
            return new ClientWebSocketTransport(EnvelopeSerializer);
        }

        protected override ITransportListener CreateTransportListener()
        {
            return new WebSocketTransportListener(new[] { Uri }, EnvelopeSerializer);
        }
    }
}
