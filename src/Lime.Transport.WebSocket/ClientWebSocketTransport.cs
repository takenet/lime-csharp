using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;

namespace Lime.Transport.WebSocket
{
    public class ClientWebSocketTransport : TransportBase, ITransport
    {
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly ClientWebSocket _clientWebSocket;
        private readonly SemaphoreSlim _receiveSemaphore;
        private readonly SemaphoreSlim _sendSemaphore;
        private readonly JsonBuffer _jsonBuffer;

        public ClientWebSocketTransport(IEnvelopeSerializer envelopeSerializer, ITraceWriter traceWriter = null, int bufferSize = 8192)
        {
            _envelopeSerializer = envelopeSerializer;
            _traceWriter = traceWriter;
            _clientWebSocket = new ClientWebSocket();
            _receiveSemaphore = new SemaphoreSlim(1);
            _sendSemaphore = new SemaphoreSlim(1);
            _jsonBuffer = new JsonBuffer(bufferSize);
        }

        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (_clientWebSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("The connection was not initialized. Call OpenAsync first.");
            }

            var envelopeJson = _envelopeSerializer.Serialize(envelope);

            if (_traceWriter != null &&
                _traceWriter.IsEnabled)
            {
                await _traceWriter.TraceAsync(envelopeJson, DataOperation.Send).ConfigureAwait(false);
            }

            var jsonBytes = Encoding.UTF8.GetBytes(envelopeJson);

            await _sendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await _clientWebSocket.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, true,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _sendSemaphore.Release();
            }            
        }

        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (_clientWebSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("The connection was not initialized. Call OpenAsync first.");
            }

            await _receiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {                
                var buffer = new ArraySegment<byte>(_jsonBuffer.Buffer);
                while (true)
                {
                    var receiveResult = await _clientWebSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                    _jsonBuffer.BufferCurPos += receiveResult.Count;
                    if (receiveResult.EndOfMessage) break;
                }

                byte[] jsonBytes;
                if (_jsonBuffer.TryExtractJsonFromBuffer(out jsonBytes))
                {
                    var envelopeJson = Encoding.UTF8.GetString(jsonBytes);

                    if (_traceWriter != null &&
                        _traceWriter.IsEnabled)
                    {
                        await _traceWriter.TraceAsync(envelopeJson, DataOperation.Receive).ConfigureAwait(false);
                    }

                    return _envelopeSerializer.Deserialize(envelopeJson);
                }
            }
            finally
            {
                _receiveSemaphore.Release();
            }

            return null;
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            return _clientWebSocket.ConnectAsync(uri, cancellationToken);
        }

        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            return _clientWebSocket.CloseAsync(WebSocketCloseStatus.Empty, "The session was finished", cancellationToken);
        }

        public override bool IsConnected => _clientWebSocket.State == WebSocketState.Open;

        public override IReadOnlyDictionary<string, object> Options => new Dictionary<string, object>()
        {
            {nameof(ClientWebSocket.SubProtocol), _clientWebSocket.SubProtocol},
            {nameof(ClientWebSocket.CloseStatusDescription), _clientWebSocket.CloseStatusDescription},
            {nameof(ClientWebSocket.CloseStatus), _clientWebSocket.CloseStatus},
            {nameof(System.Net.WebSockets.WebSocket.DefaultKeepAliveInterval), System.Net.WebSockets.WebSocket.DefaultKeepAliveInterval},
            {nameof(ClientWebSocketOptions.KeepAliveInterval), _clientWebSocket.Options?.KeepAliveInterval},

        };
    }
}
