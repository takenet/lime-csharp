using System;
using System.Threading.Channels;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using Lime.Protocol.UnitTests.Network;
using Microsoft.AspNetCore.SignalR.Client;
using NUnit.Framework;

namespace Lime.Transport.SignalR.UnitTests
{
    [TestFixture]
    public class SignalRTransportListenerTests : TransportListenerTestsBase
    {
        private Channel<string> EnvelopeChannel { get; } = Channel.CreateUnbounded<string>();

        protected override ITransport CreateClientTransport()
        {
            var hubConnection = new HubConnectionBuilder().WithUrl(CreateListenerUri().ToString() + "envelope").WithAutomaticReconnect().Build();
            hubConnection.On<string>("FromServer", async envelope =>
            {
                await EnvelopeChannel.Writer.WriteAsync(envelope);
            });
            
            return new ClientSignalRTransport(EnvelopeChannel, EnvelopeSerializer, hubConnection: hubConnection);
        }

        protected override Uri CreateListenerUri() => new Uri("http://localhost:57812");

        protected override ITransportListener CreateTransportListener()
            => new SignalRTransportListener(new[] { ListenerUri }, EnvelopeSerializer, traceWriter: TraceWriter.Object);
    }
}