using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using static Lime.Transport.SignalR.SignalRTransportListener;

namespace Lime.Transport.SignalR
{
    public abstract class SignalRTransport : TransportBase
    {
        private readonly ChannelReader<string> _envelopeChannel;
        private bool _isConnected;

        internal SignalRTransport(ChannelReader<string> envelopeChannel, IEnvelopeSerializer envelopeSerializer, ITraceWriter traceWriter = null)
        {

            TraceWriter = traceWriter;
            _envelopeChannel = envelopeChannel;
            EnvelopeSerializer = envelopeSerializer;
        }

        protected IEnvelopeSerializer EnvelopeSerializer { get; }
        protected ITraceWriter TraceWriter { get; }

        public override bool IsConnected => _isConnected;

        public override Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope is null)
            {
                return Task.FromException(new ArgumentNullException(nameof(envelope)));
            }

            return Task.CompletedTask;
        }

        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            ThrowIfClosed();

            // TODO check if task is already completed?
            var envelopeSerialized = await _envelopeChannel.ReadAsync(cancellationToken);
            // TODO offer ValueTask-returning alternative?
            await TraceWriter.TraceIfEnabledAsync(envelopeSerialized, DataOperation.Receive).ConfigureAwait(false);

            return EnvelopeSerializer.Deserialize(envelopeSerialized);
        }

        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            _isConnected = false;
            return Task.CompletedTask;
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("A valid HTTP or HTTPS URL must be provided", nameof(uri));
            }

            _isConnected = true;
            return Task.CompletedTask;
        }

        protected virtual void ThrowIfClosed()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("The connection is not open.");
            }
        }
    }
}
