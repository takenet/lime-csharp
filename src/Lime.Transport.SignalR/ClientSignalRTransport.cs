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
    public sealed class ClientSignalRTransport : SignalRTransport, IDisposable, IAsyncDisposable
    {
        private HubConnection _hubConnection;

        public ClientSignalRTransport(
            ChannelReader<string> envelopeChannel,
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null,
            HubConnection hubConnection = null) : base(envelopeChannel,
                                                    envelopeSerializer,
                                                    traceWriter)
        {
            _hubConnection = hubConnection;
        }

        public override string RemoteEndPoint
        {
            get
            {
                dynamic transport = _hubConnection.AsDynamic()._state.CurrentConnectionStateUnsynchronized.Connection._transport;
                var transportName = ((object)transport).GetType().Name;
                if (transportName.Contains("WebSocket", StringComparison.InvariantCulture))
                    return string.Empty;

                var webSocket = transport._webSocket._innerWebSocket._webSocket;
                return webSocket._stream._connection._socket.RemoteEndPoint.ToString();
            }
        }

        public override string LocalEndPoint
        {
            get
            {
                dynamic transport = _hubConnection.AsDynamic()._state.CurrentConnectionStateUnsynchronized.Connection._transport;
                var transportName = ((object)transport).GetType().Name;
                if (transportName.Contains("WebSocket", StringComparison.InvariantCulture))
                    return string.Empty;

                var webSocket = transport._webSocket._innerWebSocket._webSocket;
                return webSocket._stream._connection._socket.LocalEndPoint.ToString();
            }
        }

        public override IReadOnlyDictionary<string, object> Options => new Dictionary<string, object>
        {
            [nameof(_hubConnection.HandshakeTimeout)] = _hubConnection.HandshakeTimeout,
            [nameof(_hubConnection.ServerTimeout)] = _hubConnection.ServerTimeout,
            [nameof(_hubConnection.KeepAliveInterval)] = _hubConnection.KeepAliveInterval
        };

        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            ThrowIfClosed();

            return await base.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            ThrowIfClosed();

            await base.SendAsync(envelope, cancellationToken).ConfigureAwait(false);

            string envelopeSerialized = EnvelopeSerializer.Serialize(envelope);
            await TraceWriter.TraceIfEnabledAsync(envelopeSerialized, DataOperation.Send).ConfigureAwait(false);

            await _hubConnection.SendAsync("FromClient", envelopeSerialized, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            await _hubConnection.StopAsync(cancellationToken).ConfigureAwait(false);
            await base.PerformCloseAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override async Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            await base.PerformOpenAsync(uri, cancellationToken).ConfigureAwait(false);

            _hubConnection ??= new HubConnectionBuilder()
                .WithUrl(uri.ToString())
                .WithAutomaticReconnect()
                .Build();

            await _hubConnection.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override void ThrowIfClosed()
        {
            ThrowIfDisposed();
            base.ThrowIfClosed();
        }

        #region IDisposable Support
        private bool _disposed;

        public void Dispose()
        {
            Task.Run(_hubConnection.DisposeAsync).Wait();
            _disposed = true;
        }

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
