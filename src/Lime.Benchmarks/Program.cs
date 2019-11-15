using BenchmarkDotNet.Running;
using System;

namespace Lime.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<WebSocketTransportBenchmark>();
            //var summary = BenchmarkRunner.Run<TcpTransportBenchmark>();
            var summary = BenchmarkRunner.Run<PipeTcpTransportBenchmark>();
        }
    }
}
