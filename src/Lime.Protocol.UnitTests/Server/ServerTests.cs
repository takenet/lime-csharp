using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Server;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Server
{
    [TestFixture]
    public class ServerTests
    {
        private FakeTransportListener _transportListener;
        
        private INodeRegistry _nodeRegistry;
        private Node _localNode;
        private TimeSpan _sendTimeout;
        private SessionCompression[] _enabledCompressionOptions;
        private SessionEncryption[] _enabledEncryptionOptions;
        private AuthenticationScheme[] _schemeOptions;
        private Func<ITransport, IServerChannel> _serverChannelFactory;
        private Func<Identity, Authentication, CancellationToken, Task<AuthenticationResult>> _authenticator;
        private Func<IChannelInformation, IChannelListener> _channelListenerFactory;
        private List<(IChannelInformation, FakeChannelListener)> _channelListenerList;
        private CancellationTokenSource _cts;

        [SetUp]
        public void SetUp()
        {
            _transportListener = new FakeTransportListener(new []{ new Uri("fake://localhost") });
            
            _nodeRegistry = new NodeRegistry();

            _localNode = Dummy.CreateNode();
            _sendTimeout = TimeSpan.FromSeconds(30);
            _enabledCompressionOptions = new [] {SessionCompression.None};
            _enabledEncryptionOptions = new [] {SessionEncryption.None, SessionEncryption.TLS};
            _schemeOptions = new[] {AuthenticationScheme.Guest, AuthenticationScheme.Plain, AuthenticationScheme.Transport};
            
            _serverChannelFactory = t => new ServerChannel(EnvelopeId.NewId(), _localNode, t, _sendTimeout);
            _authenticator = (node, authentication, cancellationToken) => Task.FromResult(new AuthenticationResult(DomainRole.Member));
            
            _channelListenerList = new List<(IChannelInformation, FakeChannelListener)>();
            _channelListenerFactory = c =>
            {
                var channelListener = new FakeChannelListener();
                _channelListenerList.Add((c, channelListener));
                return channelListener;
            };
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        }

        [TearDown]
        public void TearDown()
        {
            _cts.Dispose();
        }
        
        private Protocol.Server.Server GetTarget()
        {
            return new Protocol.Server.Server(
                _transportListener,
                _serverChannelFactory,
                _enabledCompressionOptions,
                _enabledEncryptionOptions,
                _schemeOptions,
                _authenticator,
                _channelListenerFactory,
                _nodeRegistry);
        }

        [Test]
        public async Task StartAsync_NotStarted_ShouldStartTransportListener()
        {
            // Arrange
            var target = GetTarget();
            
            // Act
            await target.StartAsync(_cts.Token);
            
            // Assert 
            _transportListener.Started.ShouldBeTrue();
        }
        
        [Test]
        public async Task StartAsync_AlreadyStarted_ThrowsInvalidOperationException()
        {
            // Arrange
            var target = GetTarget();
            
            // Act
            await target.StartAsync(_cts.Token);
            await target.StartAsync(_cts.Token).ShouldThrowAsync<InvalidOperationException>();
        }
        
        [Test]
        public async Task StopAsync_Started_ShouldStopTransportListener()
        {
            // Arrange
            var target = GetTarget();
            await target.StartAsync(_cts.Token);
            
            // Act
            await target.StopAsync(_cts.Token);
            
            // Assert 
            _transportListener.Started.ShouldBeFalse();
        }        
        
        [Test]
        public async Task StopAsync_AlreadyStopped_ShouldStopTransportListener()
        {
            // Arrange
            var target = GetTarget();
            await target.StartAsync(_cts.Token);
            
            // Act
            await target.StopAsync(_cts.Token);
            await target.StopAsync(_cts.Token).ShouldThrowAsync<InvalidOperationException>();
        }
        
        [Test]
        public async Task StopAsync_NotStarted_ThrowsInvalidOperationException()
        {
            // Arrange
            var target = GetTarget();
            
            // Act
            await target.StopAsync(_cts.Token).ShouldThrowAsync<InvalidOperationException>();
        }
        
        [Test]
        public async Task AcceptTransportAsync_NewListenerTransport_ConsumeChannel()
        {
            // Arrange
            var target = GetTarget();
            await target.StartAsync(_cts.Token);
            
            // Act
            
            
        }
        
        private class FakeTransportListener : ITransportListener
        {
            public FakeTransportListener(Uri[] listenerUris)
            {
                ListenerUris = listenerUris;
                TransportChannel = Channel.CreateUnbounded<ITransport>();
            }

            public Uri[] ListenerUris { get; }
            
            public bool Started { get; private set; }
            
            public bool AcceptPending { get; private set; }
            
            public Channel<ITransport> TransportChannel { get; }
            
            public Task StartAsync(CancellationToken cancellationToken)
            {
                Started = true;
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                Started = false;
                return Task.CompletedTask;
            }
            
            public async Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
            {
                AcceptPending = true;
                try
                {
                    return await TransportChannel.Reader.ReadAsync(cancellationToken).AsTask();
                }
                finally
                {
                    AcceptPending = false;
                }
            }
        }

        private class FakeChannelListener : IChannelListener
        {
            private readonly ChannelListener _channelListener;
            
            public FakeChannelListener()
            {
                MessageChannel = Channel.CreateUnbounded<Message>();
                NotificationChannel = Channel.CreateUnbounded<Notification>();
                CommandChannel = Channel.CreateUnbounded<Command>();
                _channelListener = new ChannelListener(
                    async (message, token) =>
                    {
                        await MessageChannel.Writer.WriteAsync(message, token);
                        return true;
                    },
                    async (notification, token) =>
                    {
                        await NotificationChannel.Writer.WriteAsync(notification, token);
                        return true;
                    },
                    async (command, token) =>
                    {
                        await CommandChannel.Writer.WriteAsync(command, token);
                        return true;
                    });
            }

            public Channel<Message> MessageChannel { get; }
            
            public Channel<Notification> NotificationChannel { get; }
            
            public Channel<Command> CommandChannel { get; }
            
            public IEstablishedReceiverChannel ReceiverChannel { get; private set; }
            
            public bool Started { get; private set; }

            public Task<Message> MessageListenerTask => _channelListener.MessageListenerTask;

            public Task<Notification> NotificationListenerTask => _channelListener.NotificationListenerTask;

            public Task<Command> CommandListenerTask => _channelListener.CommandListenerTask;

            public void Start(IEstablishedReceiverChannel channel)
            {
                _channelListener.Start(channel);
                ReceiverChannel = channel;
                Started = true;
            }
            
            public void Stop()
            {
                _channelListener.Stop();
                Started = false;
            }
        }
    }
}