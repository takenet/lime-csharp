using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests;
using Lime.Protocol.UnitTests.Common.Network;
using Microsoft.AspNetCore.SignalR.Client;
using NUnit.Framework;
using Shouldly;

namespace Lime.Transport.SignalR.UnitTests
{
    [TestFixture]
    public class SignalRTransportTests : TransportTestsBase
    {
        private Channel<string> EnvelopeChannel { get; } = Channel.CreateUnbounded<string>();

        protected override ITransport CreateClientTransport(IEnvelopeSerializer envelopeSerializer)
        {
            var hubConnection = new HubConnectionBuilder().WithUrl(CreateListenerUri().ToString() + "envelope").WithAutomaticReconnect().Build();
            hubConnection.On<string>("FromServer", async envelope =>
            {
                await EnvelopeChannel.Writer.WriteAsync(envelope);
            });

            return new ClientSignalRTransport(EnvelopeChannel, envelopeSerializer, hubConnection: hubConnection);
        }

        protected override Uri CreateListenerUri() => new Uri("http://localhost:57813");

        protected override ITransportListener CreateTransportListener(Uri uri, IEnvelopeSerializer envelopeSerializer)
            => new SignalRTransportListener(new[] { uri }, envelopeSerializer, traceWriter: TraceWriter);

        [Test]
        public override async Task LocalEndPoint_ConnectedTransport_ShouldEqualsClientRemoteEndPoint()
        {
            // Arrange
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();

            // Act
            var actual = serverTransport.LocalEndPoint;

            // Assert
            var expected = clientTransport.RemoteEndPoint;
            actual.ShouldBeOneOf(expected, "");
        }

        [Test]
        public override async Task RemoteEndPoint_ConnectedTransport_ShouldEqualsClientLocalEndPoint()
        {
            // Arrange
            var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();

            // Act
            var actual = serverTransport.RemoteEndPoint;

            // Assert
            var expected = clientTransport.LocalEndPoint;
            actual.ShouldBeOneOf(expected, "");
        }

        //[Test]
        //public async Task LimeSessionIsMaintainedAfterReconnect()
        //{
        //    // Arrange            
        //    var message = Dummy.CreateMessage(Dummy.CreatePlainDocument());
        //    var (clientTransport, serverTransport) = await GetAndOpenTargetsAsync();

        //    // Act
        //    serverTransport.CloseAsync
        //    await clientTransport.SendAsync(message, CancellationToken);
        //    _ = await serverTransport.ReceiveAsync(CancellationToken);



        //    // Assert
        //    actual.ShouldNotBeNull();

        //    var actualMessage = actual.ShouldBeOfType<Message>();
        //    CompareMessages(message, actualMessage);
        //}
    }
}
