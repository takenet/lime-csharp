using BenchmarkDotNet.Running;
using System;

namespace Lime.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<WebSocketTransportBenchmark>();
            BenchmarkRunner.Run<TcpTransportBenchmark>();
            BenchmarkRunner.Run<PipeTcpTransportBenchmark>();
            BenchmarkRunner.Run<DeserializeBenchmark>();
        }
    }
}
