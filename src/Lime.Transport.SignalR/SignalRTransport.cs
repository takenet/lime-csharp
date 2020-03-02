using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Microsoft.AspNetCore.SignalR;
using static Lime.Transport.SignalR.SignalRTransportListener;

namespace Lime.Transport.SignalR
{
    internal class SignalRTransport : TransportBase
    {
        private readonly IHubContext<EnvelopeHub> _hubContext;
        private readonly string _connectionId;
        private readonly ITraceWriter _traceWriter;
        private Channel<string> _envelopeChannel;
        private readonly IEnvelopeSerializer _envelopeSerializer;

        internal SignalRTransport(IHubContext<EnvelopeHub> hub, string connectionId, Channel<string> envelopeChannel, IEnvelopeSerializer envelopeSerializer, ITraceWriter traceWriter)
        {
            _hubContext = hub;
            _connectionId = connectionId;
            _traceWriter = traceWriter;
            _envelopeChannel = envelopeChannel;
            _envelopeSerializer = envelopeSerializer;
        }

        public override bool IsConnected => _hubContext.Clients.Client(_connectionId) != null; // TODO verify that this works

        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            var envelopeSerialized = await _envelopeChannel.Reader.ReadAsync(cancellationToken);
            await _traceWriter.TraceIfEnabledAsync(envelopeSerialized, DataOperation.Receive).ConfigureAwait(false);
            return _envelopeSerializer.Deserialize(envelopeSerialized);
        }

        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            string envelopeSerialized = _envelopeSerializer.Serialize(envelope);
            await _traceWriter.TraceIfEnabledAsync(envelopeSerialized, DataOperation.Send).ConfigureAwait(false);
            var client = _hubContext.Clients.Client(_connectionId);

            await client.SendAsync("FromServer", envelopeSerialized).ConfigureAwait(false);
        }

        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #region IDisposable Support
        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
