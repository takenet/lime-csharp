using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Messaging;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.LoadTests
{
    public abstract class TransportLoadTestsBase
    {
        private Uri _uri;
        private IEnvelopeSerializer _envelopeSerializer;
        private ITransportListener _transportListener;
        private ITransport _clientTransport;
        private ITransport _serverTransport;
        private CancellationToken _cancellationToken;
        private CancellationTokenSource _cts;

        [SetUp]
        public async Task SetUp()
        {
            _uri = CreateUri();
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            _cancellationToken = _cts.Token;
            _envelopeSerializer = new EnvelopeSerializer(new DocumentTypeResolver().WithMessagingDocuments());
            _transportListener = CreateTransportListener(_uri, _envelopeSerializer);
            await _transportListener.StartAsync(_cancellationToken);
            var serverTcpTransportTask = _transportListener.AcceptTransportAsync(_cancellationToken);
            _clientTransport = new SynchronizedTransportDecorator(CreateClientTransport(_envelopeSerializer));
            await _clientTransport.OpenAsync(_uri, _cancellationToken);
            _serverTransport = new SynchronizedTransportDecorator(await serverTcpTransportTask);
            await _serverTransport.OpenAsync(_uri, _cancellationToken);
        }

        [TearDown]
        public async Task TearDown()
        {
            _cts.Dispose();
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                await Task.WhenAll(
                    _serverTransport.CloseAsync(cts.Token),
                    _clientTransport.CloseAsync(cts.Token));

                await _transportListener.StopAsync(cts.Token);
            }
            
            _clientTransport.DisposeIfDisposable();
            _serverTransport.DisposeIfDisposable();
            _transportListener.DisposeIfDisposable();
        }

        protected abstract Uri CreateUri();

        protected abstract ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer);

        protected abstract ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer);

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


            // Act
            var receivedEnvelopes = Enumerable
                .Range(0, count)
                .Select(i => _serverTransport.ReceiveAsync(_cancellationToken))
                .ToArray();

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
            var receivedEnvelopes = Enumerable
                .Range(0, count)
                .Select(i => _serverTransport.ReceiveAsync(_cancellationToken))
                .ToArray();

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
        public async Task SendHugeEnvelope()
        {
            try
            {
                var sw = Stopwatch.StartNew();

                var serializer = new EnvelopeSerializer(new DocumentTypeResolver().WithMessagingDocuments());

                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
                var content = File.ReadAllLines(Path.Combine(path, "huge.json"));
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
        
        [Test]
        public async Task ReceiveHugeEnvelopeWithoutCorruptingChars()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
            var content = File.ReadAllLines(Path.Combine(path, "builder.json"));
            var serializer = new DocumentSerializer(new DocumentTypeResolver().WithMessagingDocuments());
            var envelope = serializer.Deserialize(string.Join("", content), MediaType.ApplicationJson);

            var count = 50;
            Parallel.For(0, count, async (x) =>
            {
                await _clientTransport.SendAsync(new Command
                {
                    Id = x.ToString(),
                    Resource = envelope,
                    To = "postmaster@msging.net"
                }, _cancellationToken);
            });

            for (int i = 0; i < count; i++)
            {
                var response = await _serverTransport.ReceiveAsync(_cancellationToken);
                var json = JsonConvert.SerializeObject(((Command)response).Resource);
                json.ShouldNotContain("�");
            }
        }
    }
}