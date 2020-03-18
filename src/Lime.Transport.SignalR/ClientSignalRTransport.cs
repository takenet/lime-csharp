using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Microsoft.AspNetCore.SignalR.Client;

namespace Lime.Transport.SignalR
{
    /// <summary>
    /// Provides methods for client nodes to communicate with a server using SignalR as the underlying transport mechanism.
    /// </summary>
    /// <inheritdoc />
    public sealed class ClientSignalRTransport : SignalRTransport, IDisposable, IAsyncDisposable
    {
        private const string FROM_CLIENT_METHOD = "FromClient";
        private const string HUB_NAME = "envelope";
        private const string FROM_SERVER_METHOD = "FromServer";
        private const string CLOSE_METHOD = "Close";
        private HubConnection _hubConnection;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSignalRTransport"/> class.
        /// </summary>
        /// <param name="envelopeSerializer">The serializer for envelopes exchanged in the transport connections.</param>
        /// <param name="traceWriter">A sink for tracing messages. Default <c>null</c>.</param>
        public ClientSignalRTransport(
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null) 
            : base(
                  Channel.CreateUnbounded<string>(),
                  envelopeSerializer,
                  traceWriter)
        {
        }

        /// <summary>
        /// Gets the remote endpoint address when the underlying transport is WebSockets; Otherwise, returns an empty string.
        /// </summary>
        public override string RemoteEndPoint
        {
            get
            {
                try
                {
                    return _hubConnection?.GetRemoteEndpoint()?.ToString() ?? string.Empty;
                }
                catch (MissingMemberException ex)
                {
                    TraceWriter.TraceIfEnabledAsync(ex.ToString(), DataOperation.Error);
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the local endpoint address when the underlying transport is WebSockets; Otherwise, returns an empty string.
        /// </summary>
        public override string LocalEndPoint
        {
            get
            {
                try
                {
                    return _hubConnection?.GetLocalEndpoint()?.ToString() ?? string.Empty;
                }
                catch (MissingMemberException ex)
                {
                    TraceWriter.TraceIfEnabledAsync(ex.ToString(), DataOperation.Error);
                    return string.Empty;
                }
            }
        }

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, object> Options => _hubConnection != null ? 
            new Dictionary<string, object>
            {
                [nameof(HubConnection.HandshakeTimeout)] = _hubConnection.HandshakeTimeout,
                [nameof(HubConnection.ServerTimeout)] = _hubConnection.ServerTimeout,
                [nameof(HubConnection.KeepAliveInterval)] = _hubConnection.KeepAliveInterval
            }
            : new Dictionary<string, object>();

        /// <inheritdoc />
        /// <exception cref="ObjectDisposedException">This instance has already been disposed.</exception>
        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();

            return await base.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        /// <exception cref="ObjectDisposedException">This instance has already been disposed.</exception>
        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();

            await base.SendAsync(envelope, cancellationToken).ConfigureAwait(false);

            string serializedEnvelope = EnvelopeSerializer.Serialize(envelope);
            await TraceWriter.TraceIfEnabledAsync(serializedEnvelope, DataOperation.Send).ConfigureAwait(false);

            await _hubConnection.SendAsync(FROM_CLIENT_METHOD, serializedEnvelope, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        /// <exception cref="ObjectDisposedException">This instance has already been disposed.</exception>
        protected override async Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            await _hubConnection.StopAsync(cancellationToken).ConfigureAwait(false);
            await base.PerformCloseAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        /// <exception cref="ObjectDisposedException">This instance has already been disposed.</exception>
        protected override async Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            await base.PerformOpenAsync(uri, cancellationToken).ConfigureAwait(false);

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(uri.ToString() + HUB_NAME)
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<string>(FROM_SERVER_METHOD, async envelope =>
            {
                await EnvelopeChannel.Writer.WriteAsync(envelope).ConfigureAwait(false);
            });

            _hubConnection.On(CLOSE_METHOD, async () =>
            {
                // awaiting here will cause a deadlock
                // https://github.com/dotnet/aspnetcore/issues/19750
                _ = CloseAsync(CancellationToken.None);
            });

            await _hubConnection.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> when the this instance has been disposed or
        /// an <see cref="InvalidOperationException"/> when the transport is not connected.
        /// </summary>
        protected override void ThrowIfNotConnected()
        {
            ThrowIfDisposed();
            base.ThrowIfNotConnected();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Task.Run(_hubConnection.DisposeAsync).Wait();
            _disposed = true;
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await _hubConnection.DisposeAsync().ConfigureAwait(false);
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
