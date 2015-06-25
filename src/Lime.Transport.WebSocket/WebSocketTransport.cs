using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;

namespace Lime.Transport.WebSocket
{
    public class WebSocketTransport : TransportBase, ITransport
    {
        private readonly vtortola.WebSockets.WebSocket _webSocket;
        private readonly IEnvelopeSerializer _envelopeSerializer;

        internal WebSocketTransport(vtortola.WebSockets.WebSocket webSocket, IEnvelopeSerializer envelopeSerializer)
        {
            if (webSocket == null) throw new ArgumentNullException("webSocket");
            _webSocket = webSocket;
            _envelopeSerializer = envelopeSerializer;
        }

        public override Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task OpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
