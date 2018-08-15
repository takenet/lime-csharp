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
    public class WebsocketTransportBenchmark
    {
        private Uri _uri;
        private CancellationToken _cancellationToken;
        private IEnvelopeSerializer _envelopeSerializer;
        private ITransportListener _transportListener;
        private ITransport _clientTransport;
        private ITransport _serverTransport;

        private Message _message;

        public WebsocketTransportBenchmark()
        {
            _uri = new Uri("ws://localhost:8081");
            _cancellationToken = TimeSpan.FromSeconds(60).ToCancellationToken();
            _envelopeSerializer = new EnvelopeSerializer(new DocumentTypeResolver().WithMessagingDocuments());
            _transportListener = new WebSocketTransportListener(_uri, null, _envelopeSerializer, null, webSocketMessageType: System.Net.WebSockets.WebSocketMessageType.Text);
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

            _clientTransport = new ClientWebSocketTransport(_envelopeSerializer, null, webSocketMessageType: System.Net.WebSockets.WebSocketMessageType.Text);
            await _clientTransport.OpenAsync(_uri, _cancellationToken);

            _serverTransport = (WebSocketTransport)await serverTcpTransportTask;
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
