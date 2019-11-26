using BenchmarkDotNet.Attributes;
using Lime.Messaging;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Benchmarks
{
    public abstract class TransportBenchmarkBase
    {
        protected TransportBenchmarkBase()
        {
            Uri = CreateUri();
            CancellationToken = CancellationToken.None;
            EnvelopeSerializer = new EnvelopeSerializer(new DocumentTypeResolver().WithMessagingDocuments());
        }

        protected abstract Uri CreateUri();
        public Uri Uri { get; }
        
        public CancellationToken CancellationToken { get; }
        
        public IEnvelopeSerializer EnvelopeSerializer { get; }
        
        public ITransportListener TransportListener { get; private set; }

        public ITransport ClientTransport { get; private set; }
        
        public ITransport ServerTransport { get; private set; }
        
        public SynchronizedTransportDecorator SynchronizedClientTransport { get; private set; }
        
        public SynchronizedTransportDecorator SynchronizedServerTransport { get; private set; }
        
        public Message Message { get; private set; }
        
        [GlobalSetup]
        public void Setup()
        {
            SetupAsync().Wait();
        }
        
        [GlobalCleanup]
        public void Cleanup()
        {
            CleanupAsync().Wait();
        }

        [Benchmark]
        public Task SendReceiveAsync()
        {
            var receiveTask = ServerTransport.ReceiveAsync(CancellationToken);
            var sendTask = ClientTransport.SendAsync(Message, CancellationToken);
            return Task.WhenAll(receiveTask, sendTask);
        }
        
        [Benchmark]
        public Task SendReceiveBatchAsync()
        {
            var tasks = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                tasks.Add(SynchronizedServerTransport.ReceiveAsync(CancellationToken));
                tasks.Add(SynchronizedClientTransport.SendAsync(Message, CancellationToken));
            }
            
            return Task.WhenAll(tasks);
        }
        
        private async Task SetupAsync()
        {
            TransportListener = CreateTransportListener();
            await TransportListener.StartAsync(CancellationToken);

            var serverTcpTransportTask = TransportListener.AcceptTransportAsync(CancellationToken);

            ClientTransport = CreateClientTransport();
            await ClientTransport.OpenAsync(Uri, CancellationToken);
            SynchronizedClientTransport = new SynchronizedTransportDecorator(ClientTransport);

            ServerTransport = await serverTcpTransportTask;
            await ServerTransport.OpenAsync(Uri, CancellationToken);
            SynchronizedServerTransport = new SynchronizedTransportDecorator(ServerTransport);

            Message = Dummy.CreateMessage(Dummy.CreateTextContent());
        }
        
        private async Task CleanupAsync()
        {
            try
            {
                await TransportListener.StopAsync(CancellationToken);
                await Task.WhenAll(
                    ServerTransport.CloseAsync(CancellationToken),
                    ClientTransport.CloseAsync(CancellationToken));
            }
            catch {}
        }
        
        protected abstract ITransport CreateClientTransport();

        protected abstract ITransportListener CreateTransportListener();
    }
}
