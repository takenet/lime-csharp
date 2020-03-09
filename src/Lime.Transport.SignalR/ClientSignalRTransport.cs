using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Microsoft.AspNetCore.SignalR.Client;
using ReflectionMagic;

namespace Lime.Transport.SignalR
{
    /// <summary>
    /// Provides methods for client nodes to communicate with the server using SignalR as the underlying transport mechanism.
    /// This class cannot be inherited.
    /// </summary>
    /// <inheritdoc />
    public sealed class ClientSignalRTransport : SignalRTransport, IDisposable, IAsyncDisposable
    {
        private HubConnection _hubConnection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSignalRTransport"/> class.
        /// </summary>
        /// <param name="envelopeSerializer">The serializer for envelopes exchanged in the transport connections.</param>
        /// <param name="traceWriter">A sink for tracing messages. Default <c>null</c>.</param>
        public ClientSignalRTransport(
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null) : base(Channel.CreateUnbounded<string>(),
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
                    dynamic transport = _hubConnection.AsDynamic()._state.CurrentConnectionStateUnsynchronized.Connection._transport;
                    var transportName = ((object)transport).GetType().Name;
                    if (transportName.Contains("WebSocket", StringComparison.InvariantCulture))
                        return string.Empty;

                    var webSocket = transport._webSocket._innerWebSocket._webSocket;
                    return webSocket._stream._connection._socket.RemoteEndPoint.ToString();
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
                    dynamic transport = _hubConnection.AsDynamic()._state.CurrentConnectionStateUnsynchronized.Connection._transport;
                    var transportName = ((object)transport).GetType().Name;
                    if (transportName.Contains("WebSocket", StringComparison.InvariantCulture))
                        return string.Empty;

                    var webSocket = transport._webSocket._innerWebSocket._webSocket;
                    return webSocket._stream._connection._socket.LocalEndPoint.ToString();
                }
                catch (MissingMemberException ex)
                {
                    TraceWriter.TraceIfEnabledAsync(ex.ToString(), DataOperation.Error);
                    return string.Empty;
                }
            }
        }

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, object> Options => new Dictionary<string, object>
        {
            [nameof(_hubConnection.HandshakeTimeout)] = _hubConnection.HandshakeTimeout,
            [nameof(_hubConnection.ServerTimeout)] = _hubConnection.ServerTimeout,
            [nameof(_hubConnection.KeepAliveInterval)] = _hubConnection.KeepAliveInterval
        };

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

            string envelopeSerialized = EnvelopeSerializer.Serialize(envelope);
            await TraceWriter.TraceIfEnabledAsync(envelopeSerialized, DataOperation.Send).ConfigureAwait(false);

            await _hubConnection.SendAsync("FromClient", envelopeSerialized, cancellationToken).ConfigureAwait(false);
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
                .WithUrl(uri.ToString() + "envelope")
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<string>("FromServer", async envelope =>
            {
                await EnvelopeChannel.Writer.WriteAsync(envelope);
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

        #region IDisposable Support
        private bool _disposed;

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

        #endregion
    }
}
