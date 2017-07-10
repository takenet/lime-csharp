using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.UnitTests;
using Moq;
using Shouldly;
using Xunit;
using Lime.Protocol.Network.UnitTests;

namespace Lime.Transport.WebSocket.UnitTests
{
    public class ServerWebSocketTransportTests : ServerTransportTestsBase<ServerWebSocketTransport, ClientWebSocketTransport, WebSocketTransportListener>
    {
        public ServerWebSocketTransportTests()
            : base(new Uri("ws://localhost:8081"))
        {

        }

        protected override ClientWebSocketTransport CreateClientTransport()
        {
            return new ClientWebSocketTransport(EnvelopeSerializer);
        }

        protected override WebSocketTransportListener CreateTransportListener()
        {
            return new WebSocketTransportListener(ListenerUri, null, EnvelopeSerializer, TraceWriter.Object);
        }
    }
}