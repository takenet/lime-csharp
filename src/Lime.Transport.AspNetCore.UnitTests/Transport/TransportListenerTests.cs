using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol;
using Lime.Protocol.Security;
using Lime.Transport.AspNetCore.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace Lime.Transport.AspNetCore.UnitTests.Transport
{
    [TestFixture]
    public class TransportListenerTests : TestsBase
    {

        [SetUp]
        public void SetUp()
        {
            base.SetUp();
        }
        
        private TransportListener GetTarget() => new TransportListener(
            new OptionsWrapper<LimeOptions>(Options),
            ServiceScopeFactory.Object, new Logger<TransportListener>(new LoggerFactory()));


        [Test]
        public async Task ListenAsync_ValidTransport_ShouldReceiveSession()
        {
            // Arrange
            var transport = new FakeTransport();
            await transport.ReceiveBuffer.SendAsync(new Session() {State = SessionState.New});
            //await transport.ReceiveBuffer.SendAsync(new Session() {State = SessionState.Authenticating, Authentication = new GuestAuthentication()});
            var target = GetTarget();

            // Act
            try
            {
                var listenTask = target.ListenAsync(transport, CancellationTokenSource.Token);
                CancellationTokenSource.CancelAfter(250);
                await listenTask;
            }
            catch (OperationCanceledException)
            {
                
            }

            // Assert
            transport.SendBuffer.TryReceiveAll(out var envelopes).ShouldBeTrue();
            envelopes.Count.ShouldBe(1);
            var actualSession = envelopes[0].ShouldBeOfType<Session>();
            actualSession.Id.ShouldNotBeNull();
            actualSession.State.ShouldBe(SessionState.Authenticating);
        }

    }
}