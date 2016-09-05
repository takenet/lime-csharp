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
            _envelopeSerializer = new FakeEnvelopeSerializer(10);
            _tcpTransportListener = new TcpTransportListener(_uri, null, _envelopeSerializer);
            await _tcpTransportListener.StartAsync();
            var serverTcpTransportTask = _tcpTransportListener.AcceptTransportAsync(_cancellationToken);
            _clientTcpTransport = new TcpTransport(_envelopeSerializer);
            await _clientTcpTransport.OpenAsync(_uri, _cancellationToken);
            _serverTcpTransport = (TcpTransport)await serverTcpTransportTask;
            await _serverTcpTransport.OpenAsync(_uri, _cancellationToken);
        }

        [TearDown]
        public async Task TeardownAsync()
        {
            await _clientTcpTransport.CloseAsync(CancellationToken.None);
            await _serverTcpTransport.CloseAsync(CancellationToken.None);
            await _tcpTransportListener.StopAsync();
        }

        [Test]
        public async Task Send10000EnvelopesAsync()
        {
            // Arrange
            var count = 10000;
            var envelopes = Enumerable
                .Range(0, count)
                .Select(i => Dummy.CreateMessage(Dummy.CreateTextContent()));

            var receivedEnvelopes = Enumerable
                .Range(0, count)
                .Select(i => _serverTcpTransport.ReceiveAsync(_cancellationToken))
                .ToArray();

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

    public sealed class FakeEnvelopeSerializer : IEnvelopeSerializer
    {
        private readonly Envelope[] _envelopes;
        private readonly string[] _serializedEnvelopes;
        private int _serializePos;
        private int _deserializePos;

        private readonly object _serializeSyncRoot = new object();
        private readonly object _deserializeSyncRoot = new object();

        public FakeEnvelopeSerializer(int count)
        {
            _envelopes = Enumerable
                .Range(0, count)
                .Select<int, Envelope>(i =>
                {
                    if (i % 5 == 0)
                    {
                        return Dummy.CreateNotification(Event.Accepted);
                    }
                    if (i % 3 == 0)
                    {
                        return Dummy.CreateCommand(Dummy.CreateAccount());
                    }
                    if (i % 2 == 0)
                    {
                        return Dummy.CreateMessage(Dummy.CreateTextContent());
                    } 
                    return Dummy.CreateMessage(Dummy.CreateSelect());
                })
                .ToArray();

            var jsonSerializer = new JsonNetSerializer();
            _serializedEnvelopes = _envelopes.Select(e => jsonSerializer.Serialize(e)).ToArray();
        }

        public string Serialize(Envelope envelope)
        {
            lock (_serializeSyncRoot)
            {
                var value = _serializedEnvelopes[_serializePos];
                _serializePos = (_serializedEnvelopes.Length + 1) % _serializedEnvelopes.Length;
                return value;
            }
        }

        public Envelope Deserialize(string envelopeString)
        {
            lock (_deserializeSyncRoot)
            {
                var value = _envelopes[_deserializePos];
                _deserializePos = (_envelopes.Length + 1) % _envelopes.Length;
                return value;
            }
        }
    }
}
