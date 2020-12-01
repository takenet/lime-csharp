using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests;
using Moq;
using NUnit.Framework;

namespace Lime.Protocol.IntegrationTests.Client
{
    [TestFixture]
    public class OnDemandClientChannelTests
    {
        private TimeSpan _sendTimeout;
        private int _clientChannelCount = 6;
        private IEstablishedClientChannelBuilder _establishedClientChannelBuilder;
        private CancellationToken _cancellationToken;
        private IClientChannelBuilder _clientChannelBuilder;
        private FakeTransport[] _clientTransports;
        private FakeTransport[] _serverTransports;

        [SetUp]
        public void SetUp()
        {
            _sendTimeout = TimeSpan.FromSeconds(500);
            _cancellationToken = _sendTimeout.ToCancellationToken();
            BuildTransports();
        }

        private void BuildTransports()
        {
            _clientTransports = new FakeTransport[] 
            { 
                new FakeTransport("clientTransport0"),
                new FakeTransport("clientTransport1"),
                new FakeTransport("clientTransport2"),
                new FakeTransport("clientTransport3"),
                new FakeTransport("clientTransport4"),
                new FakeTransport("clientTransport5")
            };
            _serverTransports = new FakeTransport[] 
            { 
                new FakeTransport("serverTransport0"),
                //new FakeTransport("serverTransport1"),
                //new FakeTransport("serverTransport2"),
                //new FakeTransport("serverTransport3"),
                //new FakeTransport("serverTransport4"),
                //new FakeTransport("serverTransport5")
            };

            int counter = 0;
            _serverTransports[0].ReceiveBuffer = new BufferBlock<Envelope>();
            for (int i = 0; i < _clientChannelCount; i++)
            {
                _clientTransports[i].SendBuffer.LinkTo(_serverTransports[0].ReceiveBuffer);
                _clientTransports[i].ReceiveBuffer = _serverTransports[0].SendBuffer;
            }
            _clientChannelBuilder = ClientChannelBuilder.Create(() => _clientTransports[counter++], new Uri("net.tcp://localhost:8080"));
            _establishedClientChannelBuilder = new EstablishedClientChannelBuilder(_clientChannelBuilder);
        }

        [Test]
        public async Task FinishAsync_EstablishedChannel_SendFinishingAndAwaitsForFinishedSession()
        {
            // Arrange
            var target = GetTarget();
            await _serverTransports[0].OpenAsync(new Uri("net.tcp://localhost:8080"), _cancellationToken);
            for (int i = 0; i < _clientChannelCount; i++)
            {
                await _clientTransports[i].OpenAsync(new Uri("net.tcp://localhost:8080"), _cancellationToken);
                await _serverTransports[0].SendAsync(Dummy.CreateSession(SessionState.Established), default);

                var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
                await target.SendMessageAsync(message, CancellationToken.None);
                await _serverTransports[0].ReceiveAsync(CancellationToken.None);
                if(i == 0) await _serverTransports[0].ReceiveAsync(CancellationToken.None);
            }

            // Act
            var session = Dummy.CreateSession(SessionState.Finished);
            var finishingTask = target.FinishAsync(_cancellationToken);
            await _serverTransports[0].ReceiveAsync(CancellationToken.None);
            await Task.WhenAll(finishingTask, _serverTransports[0].SendAsync(session, CancellationToken.None));

            // Assert
            Assert.IsTrue(true);
        }

        private IOnDemandClientChannel GetTarget()
        {
            return new MultiplexerClientChannel(_establishedClientChannelBuilder, 6);
        }
    }

    public class FakeTransport : TransportBase, ITransport
    {
        public FakeTransport(string name)

        {
            SendBuffer = new BufferBlock<Envelope>(
                new DataflowBlockOptions()
                {
                    BoundedCapacity = 1
                });
            Name = name;
        }

        public string Name { get; set; }

        public BufferBlock<Envelope> SendBuffer { get; }

        public BufferBlock<Envelope> ReceiveBuffer { get; set; }

        public override Task SendAsync(Envelope envelope, CancellationToken cancellationToken) => SendBuffer.SendAsync(envelope, cancellationToken);

        public override Task<Envelope> ReceiveAsync(CancellationToken cancellationToken) => ReceiveBuffer.ReceiveAsync(cancellationToken);

        public override bool IsConnected => !SendBuffer.Completion.IsCompleted;

        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            SendBuffer.Complete();
            return Task.CompletedTask;
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
