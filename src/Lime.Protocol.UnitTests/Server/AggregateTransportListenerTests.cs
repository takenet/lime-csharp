using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using Lime.Protocol.Util;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Server
{
    [TestFixture]
    public class AggregateTransportListenerTests
    {
        [SetUp]
        public void SetUp()
        {
            CancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        }

        [TearDown]
        public void TearDown()
        {
            CancellationTokenSource.Dispose();
        }
        
        public CancellationTokenSource CancellationTokenSource { get; set; }

        public CancellationToken CancellationToken => CancellationTokenSource.Token;
        
        [Test]
        [Category(nameof(AggregateTransportListener.StartAsync))]
        public async Task StartAsync_SingleListener_CallStart()
        {
            // Arrange
            var transportListener1 = new Mock<ITransportListener>();
            var transportListeners = new List<ITransportListener> {transportListener1.Object};
            var capacity = -1;
            var target = new AggregateTransportListener(transportListeners, capacity);
            
            // Act
            await target.StartAsync(CancellationToken);
            
            // Assert
            transportListener1
                .Verify(t
                    => t.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        [Category(nameof(AggregateTransportListener.StartAsync))]
        public async Task StartAsync_MultipleListeners_CallStart()
        {
            // Arrange
            var transportListener1 = new Mock<ITransportListener>();
            var transportListener2 = new Mock<ITransportListener>();
            var transportListener3 = new Mock<ITransportListener>();
            var transportListeners = new List<ITransportListener>
            {
                transportListener1.Object,
                transportListener2.Object,
                transportListener3.Object
            };
            var capacity = -1;
            var target = new AggregateTransportListener(transportListeners, capacity);
            
            // Act
            await target.StartAsync(CancellationToken);
            
            // Assert
            transportListener1
                .Verify(t 
                    => t.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
            transportListener2
                .Verify(t 
                    => t.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
            transportListener3
                .Verify(t 
                    => t.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category(nameof(AggregateTransportListener.StartAsync))]
        public async Task StartAsync_ListenerThrowsException_PropagateException()
        {
            // Arrange
            var transportListener1 = new Mock<ITransportListener>();
            var transportListener2 = new Mock<ITransportListener>();
            transportListener2
                .Setup(t => t.StartAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApplicationException());
            var transportListener3 = new Mock<ITransportListener>();
            var transportListeners = new List<ITransportListener>
            {
                transportListener1.Object,
                transportListener2.Object,
                transportListener3.Object
            };
            var capacity = -1;
            var target = new AggregateTransportListener(transportListeners, capacity);
            
            // Act
            await target.StartAsync(CancellationToken).ShouldThrowAsync<ApplicationException>();
        }
        
        [Test]
        [Category(nameof(AggregateTransportListener.StopAsync))]
        public async Task StopAsync_SingleListener_CallStop()
        {
            // Arrange
            var transportListener1 = new Mock<ITransportListener>();
            var transportListeners = new List<ITransportListener> {transportListener1.Object};
            var capacity = -1;
            var target = new AggregateTransportListener(transportListeners, capacity);
            
            // Act
            await target.StopAsync(CancellationToken);
            
            // Assert
            transportListener1
                .Verify(t
                    => t.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        [Category(nameof(AggregateTransportListener.StopAsync))]
        public async Task StopAsync_MultipleListeners_CallStop()
        {
            // Arrange
            var transportListener1 = new Mock<ITransportListener>();
            var transportListener2 = new Mock<ITransportListener>();
            var transportListener3 = new Mock<ITransportListener>();
            var transportListeners = new List<ITransportListener>
            {
                transportListener1.Object,
                transportListener2.Object,
                transportListener3.Object
            };
            var capacity = -1;
            var target = new AggregateTransportListener(transportListeners, capacity);
            
            // Act
            await target.StopAsync(CancellationToken);
            
            // Assert
            transportListener1
                .Verify(t 
                    => t.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
            transportListener2
                .Verify(t 
                    => t.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
            transportListener3
                .Verify(t 
                    => t.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category(nameof(AggregateTransportListener.StopAsync))]
        public async Task StopAsync_ListenerThrowsException_PropagateException()
        {
            // Arrange
            var transportListener1 = new Mock<ITransportListener>();
            var transportListener2 = new Mock<ITransportListener>();
            transportListener2
                .Setup(t => t.StopAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApplicationException());
            var transportListener3 = new Mock<ITransportListener>();
            var transportListeners = new List<ITransportListener>
            {
                transportListener1.Object,
                transportListener2.Object,
                transportListener3.Object
            };
            var capacity = -1;
            var target = new AggregateTransportListener(transportListeners, capacity);
            
            // Act
            await target.StopAsync(CancellationToken).ShouldThrowAsync<ApplicationException>();
        }

        [Test]
        [Category(nameof(AggregateTransportListener.AcceptTransportAsync))]
        public async Task AcceptTransportAsync_MultipleListeners_ReceivesFromAll()
        {
            // Arrange
            var transportListener1 = new FakeTransportListener();
            var transportListener2 = new FakeTransportListener();
            var transportListener3 = new FakeTransportListener();
            var transportListeners = new List<ITransportListener>
            {
                transportListener1,
                transportListener2,
                transportListener3
            };
            var capacity = -1;
            var count = 100;

            var transports = Enumerable
                .Range(0, count)
                .Select(i => new FakeTransport(i))
                .ToList();
            
            for (int i = 0; i < transports.Count; i++)
            {
                var transport = transports[i];
                if (i % 3 == 0) await transportListener1.ChannelWriter.WriteAsync(transport, CancellationToken);
                else if (i % 3 == 1) await transportListener2.ChannelWriter.WriteAsync(transport, CancellationToken);
                else if (i % 3 == 2) await transportListener3.ChannelWriter.WriteAsync(transport, CancellationToken);
            }
            var target = new AggregateTransportListener(transportListeners, capacity);
            await target.StartAsync(CancellationToken);
            
            // Act
            var actuals = new List<ITransport>();
            for (int i = 0; i < count; i++)
            {
                actuals.Add(
                    await target.AcceptTransportAsync(CancellationToken));
            }
            
            // Assert
            actuals.Count.ShouldBe(transports.Count);
            foreach (var expected in transports)
            {
                actuals.ShouldContain(expected);
            }
        }        
        
        private class FakeTransportListener : ITransportListener
        {
            private readonly Channel<ITransport> _channel;

            public FakeTransportListener(Uri[] listenerUris = null, int capacity = -1)
            {
                _channel = ChannelUtil.CreateForCapacity<ITransport>(capacity);
                ListenerUris = listenerUris ?? new Uri[0];
            }

            public ChannelWriter<ITransport> ChannelWriter => _channel.Writer;

            public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public Uri[] ListenerUris { get; }

            public Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
                => _channel.Reader.ReadAsync(cancellationToken).AsTask();
        }

        private class FakeTransport : TransportBase
        {
            public int Id { get; }

            public FakeTransport(int id)
            {
                Id = id;
            }
            
            public override Task SendAsync(Envelope envelope, CancellationToken cancellationToken) 
                => Task.CompletedTask;

            public override Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
                => Task.FromResult<Envelope>(null);

            public override bool IsConnected { get; }
            
            protected override Task PerformCloseAsync(CancellationToken cancellationToken)
                => Task.CompletedTask;

            protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
                => Task.CompletedTask;
        }
    }
}