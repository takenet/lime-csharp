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
        protected override ITransport CreateClientTransport() => new ClientSignalRTransport(EnvelopeSerializer);

        protected override Uri CreateListenerUri() => new Uri("http://localhost:57812");

        protected override ITransportListener CreateTransportListener()
            => new SignalRTransportListener(new[] { ListenerUri }, EnvelopeSerializer, traceWriter: TraceWriter.Object);
    }
}