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
    /// <summary>
    /// Base class for SignalR transport implementation.
    /// </summary>
    /// <inheritdoc />
    public abstract class SignalRTransport : TransportBase
    {
        private bool _isConnected;

        internal SignalRTransport(Channel<string> envelopeChannel, IEnvelopeSerializer envelopeSerializer, ITraceWriter traceWriter = null)
        {

            EnvelopeChannel = envelopeChannel;
            EnvelopeSerializer = envelopeSerializer;
            TraceWriter = traceWriter;
        }

        /// <summary>
        /// Gets the channel used to communicate between the transport and SignalR.
        /// </summary>
        protected Channel<string> EnvelopeChannel { get; }

        /// <summary>
        /// Gets the <see cref="IEnvelopeSerializer"/> used by this transport.
        /// </summary>
        protected IEnvelopeSerializer EnvelopeSerializer { get; }

        /// <summary>
        /// Gets the <see cref="ITraceWriter"/> used by this transport.
        /// </summary>
        protected ITraceWriter TraceWriter { get; }

        /// <summary>
        /// Gets a value that indicates whether this transport is open and connected.
        /// </summary>
        public override bool IsConnected => _isConnected;

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="envelope"/> is <c>null</c>.</exception>
        public override Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope is null)
            {
                return Task.FromException(new ArgumentNullException(nameof(envelope)));
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when the transport is not connected.</exception>
        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();

            var envelopeSerialized = await EnvelopeChannel.Reader.ReadAsync(cancellationToken);
            await TraceWriter.TraceIfEnabledAsync(envelopeSerialized, DataOperation.Receive).ConfigureAwait(false);

            return EnvelopeSerializer.Deserialize(envelopeSerialized);
        }

        /// <inheritdoc />
        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            _isConnected = false;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="uri"/> hasn't a valid HTTP or HTTPS scheme.</exception>
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

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> when the transport is not connected.
        /// </summary>
        protected virtual void ThrowIfNotConnected()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("The connection is not open.");
            }
        }
    }
}
