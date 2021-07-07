using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol;
using Lime.Protocol.Security;
using Lime.Transport.AspNetCore.Listeners;
using Lime.Transport.AspNetCore.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using static Lime.Protocol.UnitTests.Dummy;

namespace Lime.Transport.AspNetCore.UnitTests.Transport
{
    [TestFixture]
    public class TransportListenerTests : TestsBase
    {
        [SetUp]
        public void SetUp()
        {
            base.SetUp();
            
            MessageListener = new FakeMessageListener();
            MessageListeners = new List<FakeMessageListener>();
            MessageListeners.Add(MessageListener);
            ServiceProvider
                .Setup(s => s.GetService(typeof(IEnumerable<IMessageListener>)))
                .Returns(() => MessageListeners);
            NotificationListener = new FakeNotificationListener();
            NotificationListeners = new List<FakeNotificationListener>();
            NotificationListeners.Add(NotificationListener);
            ServiceProvider
                .Setup(s => s.GetService(typeof(IEnumerable<INotificationListener>)))
                .Returns(() => NotificationListeners);
            CommandListener = new FakeCommandListener();
            CommandListeners = new List<FakeCommandListener>();
            CommandListeners.Add(CommandListener);
            ServiceProvider
                .Setup(s => s.GetService(typeof(IEnumerable<ICommandListener>)))
                .Returns(() => CommandListeners);
        }

        [TearDown]
        public new void TearDown()
        {
            base.TearDown();
        }

        public FakeMessageListener MessageListener { get; private set; }
        public List<FakeMessageListener> MessageListeners { get; private set; }
        public FakeNotificationListener NotificationListener { get; private set; }
        public List<FakeNotificationListener> NotificationListeners { get; private set; }
        public FakeCommandListener CommandListener { get; private set; }
        public List<FakeCommandListener> CommandListeners { get; private set; }
        
        private TransportListener GetTarget() => new TransportListener(
            new OptionsWrapper<LimeOptions>(Options),
            ServiceScopeFactory.Object, new Logger<TransportListener>(new LoggerFactory()));


        [Test]
        public async Task ListenAsync_ValidTransport_ShouldReceiveSession()
        {
            // Arrange
            var transport = await GetTransportAsync();
            var target = GetTarget();
            transport.SentHandlers.Add(e =>
            {
                if (e is Session {State: SessionState.Established})
                {
                    CancellationTokenSource.CancelAfter(100);
                }
                return Task.CompletedTask;
            });

            // Act
            try
            {
                await target.ListenAsync(transport, CancellationTokenSource.Token);
            }
            catch (OperationCanceledException ex) 
                when (ex.CancellationToken == CancellationTokenSource.Token)
            {
                
            }

            // Assert
            var envelopes = transport.SentEnvelopes;
            envelopes.Count.ShouldBe(3);
            var session0 = envelopes[0].ShouldBeOfType<Session>();
            session0.Id.ShouldNotBeNull();
            session0.State.ShouldBe(SessionState.Authenticating);
            var session1 = envelopes[1].ShouldBeOfType<Session>();
            session1.Id.ShouldNotBeNull();
            session1.State.ShouldBe(SessionState.Established);
            var session2 = envelopes[2].ShouldBeOfType<Session>();
            session2.Id.ShouldNotBeNull();
            session2.State.ShouldBe(SessionState.Finished);
        }
        
        [Test]
        public async Task ListenAsync_SingleMessage_ShouldInvokeListener()
        {
            // Arrange
            var transport = await GetTransportAsync();
            var message = CreateMessage(CreateTextContent());
            transport.SentHandlers.Add(async e =>
            {
                if (e is Session {State: SessionState.Established})
                {
                    await transport.ReceiveBuffer.SendAsync(message);
                    CancellationTokenSource.CancelAfter(100);
                }
            });

            var target = GetTarget();

            // Act
            try
            {
                await target.ListenAsync(transport, CancellationTokenSource.Token);
            }
            catch (OperationCanceledException ex) 
                when (ex.CancellationToken == CancellationTokenSource.Token)
            {
                
            }

            // Assert
            MessageListener.Envelopes.Count.ShouldBe(1);
            MessageListener.Envelopes[0].ShouldBe(message);
        }
        
        [Test]
        public async Task ListenAsync_SingleMessage_ShouldSetContext()
        {
            // Arrange
            var transport = await GetTransportAsync();
            var message = CreateMessage(CreateTextContent());
            transport.SentHandlers.Add(async e =>
            {
                if (e is Session {State: SessionState.Established})
                {
                    await transport.ReceiveBuffer.SendAsync(message);
                    CancellationTokenSource.CancelAfter(100);
                }
            });

            var target = GetTarget();

            // Act
            try
            {
                await target.ListenAsync(transport, CancellationTokenSource.Token);
            }
            catch (OperationCanceledException ex) 
                when (ex.CancellationToken == CancellationTokenSource.Token)
            {
                
            }

            // Assert
            ChannelContextProviders.Count.ShouldBe(1);
            var context = ChannelContextProviders[0].GetContext();
            context.ShouldNotBeNull();
        }
        
        [Test]
        public async Task ListenAsync_SingleCommand_ShouldInvokeListener()
        {
            // Arrange
            var transport = await GetTransportAsync();
            var command = CreateCommand(CreatePing());
            transport.SentHandlers.Add(async e =>
            {
                if (e is Session {State: SessionState.Established})
                {
                    await transport.ReceiveBuffer.SendAsync(command);
                    CancellationTokenSource.CancelAfter(100);
                }
            });

            var target = GetTarget();

            // Act
            try
            {
                await target.ListenAsync(transport, CancellationTokenSource.Token);
            }
            catch (OperationCanceledException ex) 
                when (ex.CancellationToken == CancellationTokenSource.Token)
            {
                
            }

            // Assert
            CommandListener.Envelopes.Count.ShouldBe(1);
            CommandListener.Envelopes[0].ShouldBe(command);
        }
        
        [Test]
        public async Task ListenAsync_SingleNotification_ShouldInvokeListener()
        {
            // Arrange
            var transport = await GetTransportAsync();
            var notification = CreateNotification(Event.Received);
            transport.SentHandlers.Add(async e =>
            {
                if (e is Session {State: SessionState.Established})
                {
                    await transport.ReceiveBuffer.SendAsync(notification);
                    CancellationTokenSource.CancelAfter(100);
                }
            });

            var target = GetTarget();

            // Act
            try
            {
                await target.ListenAsync(transport, CancellationTokenSource.Token);
            }
            catch (OperationCanceledException ex) 
                when (ex.CancellationToken == CancellationTokenSource.Token)
            {
                
            }

            // Assert
            NotificationListener.Envelopes.Count.ShouldBe(1);
            NotificationListener.Envelopes[0].ShouldBe(notification);
        }
        
        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        public async Task ListenAsync_OneEnvelopeOfEachType_ShouldInvokeListeners(int count)
        {
            // Arrange
            var transport = await GetTransportAsync();
            var messages = Enumerable.Range(0, count).Select(_ => CreateMessage(CreateTextContent())).ToArray();
            var commands = Enumerable.Range(0, count).Select(_ => CreateCommand(CreatePing())).ToArray();
            var notifications = Enumerable.Range(0, count).Select(_ => CreateNotification(Event.Received)).ToArray();
            transport.SentHandlers.Add(async e =>
            {
                if (e is Session {State: SessionState.Established})
                {
                    for (int i = 0; i < count; i++)
                    {
                        await transport.ReceiveBuffer.SendAsync(messages[i]);
                        await transport.ReceiveBuffer.SendAsync(commands[i]);
                        await transport.ReceiveBuffer.SendAsync(notifications[i]);
                    }

                    CancellationTokenSource.CancelAfter(1000);
                }
            });

            var target = GetTarget();

            // Act
            try
            {
                await target.ListenAsync(transport, CancellationTokenSource.Token);
            }
            catch (OperationCanceledException ex) 
                when (ex.CancellationToken == CancellationTokenSource.Token)
            {
                
            }

            // Assert
            MessageListener.Envelopes.Count.ShouldBe(count);
            CommandListener.Envelopes.Count.ShouldBe(count);
            NotificationListener.Envelopes.Count.ShouldBe(count);
            for (int i = 0; i < count; i++)
            {
                MessageListener.Envelopes.ShouldContain(messages[i]);
                CommandListener.Envelopes.ShouldContain(commands[i]);
                NotificationListener.Envelopes.ShouldContain(notifications[i]);
            }
        }

        private async Task<FakeTransport> GetTransportAsync()
        {
            var transport = new FakeTransport();
            await transport.OpenAsync(null, CancellationTokenSource.Token);
            transport.SentHandlers.Add(async e =>
            {
                if (e is Session {State: SessionState.Authenticating} session)
                {
                    await transport.ReceiveBuffer.SendAsync(
                        new Session()
                        {
                            Id = session.Id,
                            From = new Node(Guid.NewGuid().ToString(), "domain.local", "instance"),
                            State = SessionState.Authenticating,
                            Authentication = new GuestAuthentication()
                        });
                }
            });
            await transport.ReceiveBuffer.SendAsync(new Session() {State = SessionState.New});
            return transport;
        }
    }
}