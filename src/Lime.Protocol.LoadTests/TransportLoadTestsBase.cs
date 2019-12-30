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
            
            // Set null to avoid issues with NUnit reusing the test class instance
            _clientTransport = null;
            _serverTransport = null;
            _transportListener = null;
        }

        protected abstract Uri CreateUri();

        protected abstract ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer);

        protected abstract ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer);

        [Test]
        public async Task Send100000MessagesAsync()
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
        public async Task Send10000MessagesAsync()
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
        public async Task Send500MessagesAsync()
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
        public async Task Send200MessagesAsync()
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
        public async Task Send100HugeEnvelopesAsync()
        {
            // Arrange
            var count = 100;
            
            var serializer = new EnvelopeSerializer(new DocumentTypeResolver().WithMessagingDocuments());
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
            var content = File.ReadAllLines(Path.Combine(path, "huge.json"));
            var hugeEnvelope = (Command)serializer.Deserialize(string.Join("", content));            
            
            var envelopes = Enumerable
                .Range(0, count)
                .Select(i =>
                {
                    var command = hugeEnvelope.ShallowCopy();
                    command.Id = $"envelope_{i}";
                    return command;
                });

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
            sw.ElapsedMilliseconds.ShouldBeLessThan(count * 100);
        }        
        
        [Test]
        public async Task Send100000IntercalatedEnvelopesAsync()
        {
            // Arrange
            var count = 100000;
            var envelopes = Enumerable
                .Range(0, count)
                .Select<int, Envelope>(i =>
                {
                    switch (i % 4)
                    {
                        case 0:
                            return Dummy.CreateMessage(Dummy.CreateTextContent());
                        
                        case 1:
                            return Dummy.CreateNotification(Event.Accepted);
                        
                        case 2:
                            return Dummy.CreateCommand(Dummy.CreateJsonDocument(), CommandMethod.Set,
                                CommandStatus.Pending, Dummy.CreateRelativeLimeUri());
                        default:
                            return Dummy.CreateSession(SessionState.New);
                    }
                });

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
            receivedEnvelopes.Select(e => e.Result is Message).Count().ShouldBe(envelopes.Select(e => e is Message).Count());
            receivedEnvelopes.Select(e => e.Result is Notification).Count().ShouldBe(envelopes.Select(e => e is Notification).Count());
            receivedEnvelopes.Select(e => e.Result is Command).Count().ShouldBe(envelopes.Select(e => e is Command).Count());
            receivedEnvelopes.Select(e => e.Result is Session).Count().ShouldBe(envelopes.Select(e => e is Session).Count());
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