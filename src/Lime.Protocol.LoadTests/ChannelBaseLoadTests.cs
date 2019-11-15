using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests;
using NUnit.Framework;
using Shouldly;
using NUnit.Framework;

namespace Lime.Protocol.LoadTests
{
    [TestFixture]
    public class ChannelBaseLoadTests : IDisposable
    {
        private CancellationToken _cancellationToken;

        private FakeTransport _clientTransport;
        private FakeTransport _serverTransport;

        private TimeSpan _sendTimeout;
        private ClientChannel _clientChannel;
        private ServerChannel _serverChannel;

        public ChannelBaseLoadTests()
        {
            SetupAsync().Wait();
        }
        public async Task SetupAsync()
        {
            _cancellationToken = TimeSpan.FromSeconds(10).ToCancellationToken();

            _clientTransport = new FakeTransport();
            _serverTransport = new FakeTransport();
            _serverTransport.ReceiveBuffer = _clientTransport.SendBuffer;
            _clientTransport.ReceiveBuffer = _serverTransport.SendBuffer;

            _sendTimeout = TimeSpan.FromSeconds(60);
            _clientChannel = new ClientChannel(_clientTransport, _sendTimeout);
            _serverChannel = new ServerChannel(EnvelopeId.NewId(), Dummy.CreateNode(), _serverTransport, _sendTimeout);

            var clientEstablishSessionTask = Task.Run(() =>
                _clientChannel.EstablishSessionAsync(
                    c => c[0],
                    e => e[0],
                    Dummy.CreateIdentity(),
                    (s, a) => new GuestAuthentication(),
                    Environment.MachineName,
                    _cancellationToken),
                    _cancellationToken);

            await Task.WhenAll(
                _serverChannel.EstablishSessionAsync(
                    new [] { SessionCompression.None, },
                    new [] { SessionEncryption.None },
                    new [] { AuthenticationScheme.Guest },
                    (node, authentication) => new AuthenticationResult(null, Dummy.CreateNode()).AsCompletedTask(),
                    _cancellationToken),
                clientEstablishSessionTask);
        }

        public void Dispose()
        {
            TeardownAsync().Wait();
        }

        
        public async Task TeardownAsync()
        {
            await _serverChannel.SendFinishedSessionAsync(_cancellationToken);
            await _clientChannel.ReceiveFinishedSessionAsync(_cancellationToken);
        }


        [Test]
        public async Task Send10000EnvelopesAsync()
        {
            // Arrange
            var count = 10000;
            var messages = Enumerable
                .Range(0, count)
                .Select(i => Dummy.CreateMessage(Dummy.CreateTextContent()))
                .ToArray();
            var commands = Enumerable
                .Range(0, count)
                .Select(i => Dummy.CreateCommand(Dummy.CreatePresence()))
                .ToArray();
            var notifications = Enumerable
                .Range(0, count)
                .Select(i => Dummy.CreateNotification(Event.Consumed))
                .ToArray();
            var receivedMessages = Enumerable
                .Range(0, count)
                .Select(i => _serverChannel.ReceiveMessageAsync(_cancellationToken))
                .ToArray();
            var receivedCommands = Enumerable
                .Range(0, count)
                .Select(i => _serverChannel.ReceiveCommandAsync(_cancellationToken))
                .ToArray();
            var receivedNotifications = Enumerable
                .Range(0, count)
                .Select(i => _serverChannel.ReceiveNotificationAsync(_cancellationToken))
                .ToArray();

            // Act
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                await Task.WhenAll(
                    _clientChannel.SendMessageAsync(messages[i], _cancellationToken),
                    _clientChannel.SendCommandAsync(commands[i], _cancellationToken),
                    _clientChannel.SendNotificationAsync(notifications[i], _cancellationToken));
            }

            await Task.WhenAll(
                Task.WhenAll(receivedMessages),
                Task.WhenAll(receivedCommands),
                Task.WhenAll(receivedNotifications));
            sw.Stop();

            // Assert
            sw.ElapsedMilliseconds.ShouldBeLessThan(count * 2);
        }
        
    }

    public class FakeTransport : TransportBase, ITransport
    {                
        public FakeTransport()

        {
            SendBuffer = new BufferBlock<Envelope>(
                new DataflowBlockOptions()
                {
                    BoundedCapacity = 1
                });
        }

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
