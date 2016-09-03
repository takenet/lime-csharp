using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.UnitTests;
using Lime.Transport.Tcp;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.LoadTests.Tcp
{
    [TestFixture]
    public class TcpTransportTests
    {
        private Uri _uri;
        private CancellationToken _cancellationToken;
        private IEnvelopeSerializer _envelopeSerializer;
        private TcpTransportListener _tcpTransportListener;
        private TcpTransport _clientTcpTransport;
        private TcpTransport _serverTcpTransport;


        [SetUp]
        public async Task SetupAsync()
        {
            _uri = new Uri("net.tcp://localhost:55321");
            _cancellationToken = TimeSpan.FromSeconds(30).ToCancellationToken();
            _envelopeSerializer = new JsonNetSerializer();
            _tcpTransportListener = new TcpTransportListener(_uri, null, _envelopeSerializer);
            await _tcpTransportListener.StartAsync();

            var serverTcpTransportTask = _tcpTransportListener.AcceptTransportAsync(_cancellationToken);

            _clientTcpTransport = new TcpTransport(_envelopeSerializer);
            await _clientTcpTransport.OpenAsync(_uri, _cancellationToken);
            _serverTcpTransport = (TcpTransport)await serverTcpTransportTask;
            await _serverTcpTransport.OpenAsync(_uri, _cancellationToken);

        }

        [TearDown]
        public void Teardown()
        {
            
        }

        [Test]
        public async Task Send1000EnvelopesAsync()
        {
            // Arrange
            var count = 1000;
            var envelopes = Enumerable
                .Range(0, count)
                .Select(i => Dummy.CreateMessage(Dummy.CreateTextContent()));

            var receivedEnvelopes = Enumerable
                .Range(0, count)
                .Select(i => _serverTcpTransport.ReceiveAsync(_cancellationToken));

            // Act
            var sw = Stopwatch.StartNew();
            foreach (var envelope in envelopes)
            {
                await _clientTcpTransport.SendAsync(envelope, _cancellationToken);
            }

            await Task.WhenAll(receivedEnvelopes);
            sw.Stop();

            // Assert
            sw.ElapsedMilliseconds.ShouldBeLessThan(count * 2);
        }

    }
}
