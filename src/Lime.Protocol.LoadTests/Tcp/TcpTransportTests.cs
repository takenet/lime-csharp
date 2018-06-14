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
using Shouldly;
using NUnit.Framework;
using System.IO;
using Lime.Messaging;
using Newtonsoft.Json;
using Lime.Protocol.Network;
using Lime.Protocol.Server;

namespace Lime.Protocol.LoadTests.Tcp
{

    public class TcpTransportTests : IDisposable
    {
        private Uri _uri;
        private CancellationToken _cancellationToken;
        private IEnvelopeSerializer _envelopeSerializer;
        private ITransportListener _transportListener;
        private ITransport _clientTransport;
        private ITransport _serverTransport;

        public TcpTransportTests()
        {
            _uri = new Uri("net.tcp://localhost:55321");
            _cancellationToken = TimeSpan.FromSeconds(30).ToCancellationToken();
            _envelopeSerializer = new FakeEnvelopeSerializer(10);
            _transportListener = new TcpTransportListener(_uri, null, _envelopeSerializer);
            _transportListener.StartAsync(_cancellationToken).Wait();
            var serverTcpTransportTask = _transportListener.AcceptTransportAsync(_cancellationToken);
            _clientTransport = new TcpTransport(_envelopeSerializer);
            _clientTransport.OpenAsync(_uri, _cancellationToken).Wait();
            _serverTransport = (TcpTransport)serverTcpTransportTask.Result;
            _serverTransport.OpenAsync(_uri, _cancellationToken).Wait();
        }

        public void Dispose()
        {
            _clientTransport.CloseAsync(CancellationToken.None).Wait();
            _serverTransport.CloseAsync(CancellationToken.None).Wait();
            _transportListener.StopAsync(_cancellationToken).Wait();
        }

        [Test]
        public async Task Send100000EnvelopesAsync()
        {
            // Arrange
            var count = 100000;
            var envelopes = Enumerable
                .Range(0, count)
                .Select(i => Dummy.CreateMessage(Dummy.CreateTextContent()));

            var receivedEnvelopes = Enumerable
                .Range(0, count)
                .Select(i => _serverTransport.ReceiveAsync(_cancellationToken))
                .ToArray();

            // Act
            var sw = Stopwatch.StartNew();
            foreach (var envelope in envelopes)
            {
                await _clientTransport.SendAsync(envelope, _cancellationToken);
            }

            await Task.WhenAll(receivedEnvelopes);
            sw.Stop();

            // Assert
            sw.ElapsedMilliseconds.ShouldBeLessThan(count * 2);
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
                .Select(i => _serverTransport.ReceiveAsync(_cancellationToken))
                .ToArray();

            // Act
            var sw = Stopwatch.StartNew();
            foreach (var envelope in envelopes)
            {
                await _clientTransport.SendAsync(envelope, _cancellationToken);
            }

            await Task.WhenAll(receivedEnvelopes);
            sw.Stop();

            // Assert
            sw.ElapsedMilliseconds.ShouldBeLessThan(count * 2);
        }

        [Test]
        public async Task Send500EnvelopesAsync()
        {
            // Arrange
            var count = 500;
            var envelopes = Enumerable
                .Range(0, count)
                .Select(i => Dummy.CreateMessage(Dummy.CreateTextContent()));

            var receivedEnvelopes = Enumerable
                .Range(0, count)
                .Select(i => _serverTransport.ReceiveAsync(_cancellationToken))
                .ToArray();

            // Act
            var sw = Stopwatch.StartNew();
            foreach (var envelope in envelopes)
            {
                await _clientTransport.SendAsync(envelope, _cancellationToken);
            }

            await Task.WhenAll(receivedEnvelopes);
            sw.Stop();

            // Assert
            sw.ElapsedMilliseconds.ShouldBeLessThan(count * 2);
        }

        [Test]
        public async Task Send200EnvelopesAsync()
        {
            // Arrange
            var count = 200;
            var envelopes = Enumerable
                .Range(0, count)
                .Select(i => Dummy.CreateMessage(Dummy.CreateTextContent()));

            // Act
            var sw = Stopwatch.StartNew();
            foreach (var envelope in envelopes)
            {
                await _clientTransport.SendAsync(envelope, _cancellationToken);
            }

            var receivedEnvelopes = Enumerable
                .Range(0, count)
                .Select(i => _serverTransport.ReceiveAsync(_cancellationToken))
                .ToArray();

            await Task.WhenAll(receivedEnvelopes);
            sw.Stop();

            // Assert
            sw.ElapsedMilliseconds.ShouldBeLessThan(count * 2);
        }

        [Test]
        public async Task SendHugeEnvelope()
        {
            try
            {
                var sw = Stopwatch.StartNew();

                var serializer = new EnvelopeSerializer(new DocumentTypeResolver().WithMessagingDocuments());

                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
                var content = File.ReadAllLines(Path.Combine(path, "huge-json.txt"));
                var envelope = serializer.Deserialize(string.Join("", content));

                await _clientTransport.SendAsync(envelope, _cancellationToken);
                await _serverTransport.ReceiveAsync(_cancellationToken);
                sw.Stop();

                // Assert
                sw.ElapsedMilliseconds.ShouldBeLessThan(100);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }
    }
}
