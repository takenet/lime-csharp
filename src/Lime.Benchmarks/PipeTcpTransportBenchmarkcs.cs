using BenchmarkDotNet.Attributes;
using Lime.Messaging;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests;
using Lime.Transport.Tcp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Benchmarks
{
    [CoreJob]
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    public class PipeTcpTransportBenchmark
    {
        private Uri _uri;
        private CancellationToken _cancellationToken;
        private IEnvelopeSerializer _envelopeSerializer;
        private ITransportListener _transportListener;
        private ITransport _clientTransport;
        private ITransport _serverTransport;

        private Message _message;

        public PipeTcpTransportBenchmark()
        {
            _uri = new Uri("net.tcp://localhost:55321");
            _cancellationToken = TimeSpan.FromSeconds(60).ToCancellationToken();
            _envelopeSerializer = new EnvelopeSerializer(new DocumentTypeResolver().WithMessagingDocuments());
            _transportListener = new TcpTransportListener(_uri, null, _envelopeSerializer, usePipeTcpTransport: true);
        }

        [GlobalSetup]
        public void Setup()
        {
            SetupAsync().Wait();
        }

        
        public async Task SetupAsync()
        {
            await _transportListener.StartAsync(_cancellationToken);

            var serverTcpTransportTask = _transportListener.AcceptTransportAsync(_cancellationToken);

            _clientTransport = new PipeTcpTransport(_envelopeSerializer, null);
            await _clientTransport.OpenAsync(_uri, _cancellationToken);

            _serverTransport = (PipeTcpTransport)await serverTcpTransportTask;
            await _serverTransport.OpenAsync(_uri, _cancellationToken);

            _message = Dummy.CreateMessage(Dummy.CreateTextContent());
        }

        [Benchmark]
        public Task SendReceiveAsync()
        {
            var receiveTask = _serverTransport.ReceiveAsync(_cancellationToken);
            var sendTask = _clientTransport.SendAsync(_message, _cancellationToken);
            return Task.WhenAll(receiveTask, sendTask);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            CleanupAsync().Wait();
        }

        public async Task CleanupAsync()
        {
            await _transportListener.StopAsync(_cancellationToken);
            _serverTransport.CloseAsync(_cancellationToken);
            _clientTransport.CloseAsync(_cancellationToken);
            
        }
    }
}
