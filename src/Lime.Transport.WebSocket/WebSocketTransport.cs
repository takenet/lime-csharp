using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;

namespace Lime.Transport.WebSocket
{
    public abstract class WebSocketTransport : TransportBase, IDisposable
    {
        public const int DEFAULT_BUFFER_SIZE = 8192;
        public const int DEFAULT_MAX_BUFFER_SIZE = DEFAULT_BUFFER_SIZE * 1024;

        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);

        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly ArrayPool<byte> _arrayPool;
        private readonly int _bufferSize;
        private readonly JsonBuffer _jsonBuffer;
        private readonly SemaphoreSlim _sendSemaphore;
        private readonly SemaphoreSlim _receiveSemaphore;
        private readonly WebSocketMessageType _webSocketMessageType;

        protected WebSocketCloseStatus CloseStatus;
        protected string CloseStatusDescription;

        protected WebSocketTransport(
            System.Net.WebSockets.WebSocket webSocket,
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null,
            int bufferSize = DEFAULT_BUFFER_SIZE,
            WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text)
        {
            WebSocket = webSocket;
            _envelopeSerializer = envelopeSerializer;
            _traceWriter = traceWriter;
            _arrayPool = ArrayPool<byte>.Shared;
            _bufferSize = bufferSize;
            _jsonBuffer = new JsonBuffer(bufferSize, DEFAULT_MAX_BUFFER_SIZE, _arrayPool);
            _webSocketMessageType = webSocketMessageType;
            _sendSemaphore = new SemaphoreSlim(1);
            _receiveSemaphore = new SemaphoreSlim(1);
            CloseStatus = WebSocketCloseStatus.NormalClosure;
            CloseStatusDescription = string.Empty;
        }

        protected System.Net.WebSockets.WebSocket WebSocket { get; }

        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (WebSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("Could not send envelope: Websocket is not open");
            }

            var envelopeJson = _envelopeSerializer.Serialize(envelope);

            if (_traceWriter != null &&
                _traceWriter.IsEnabled)
            {
                await _traceWriter.TraceAsync(envelopeJson, DataOperation.Send).ConfigureAwait(false);
            }

            var jsonBytes = Encoding.UTF8.GetBytes(envelopeJson);
            await _sendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (WebSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("Could not send envelope: Websocket is not open");
            }

            try
            {
                await WebSocket.SendAsync(new ArraySegment<byte>(jsonBytes), _webSocketMessageType, true,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (WebSocketException)
            {
                await CloseWithTimeoutAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (WebSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("The connection was not initialized. Call OpenAsync first.");
            }

            await _receiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var buffer = new ArraySegment<byte>(_arrayPool.Rent(_bufferSize));
                    var receiveResult =
                        await WebSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await HandleCloseMessageAsync(receiveResult, cancellationToken);
                        break;
                    }

                    if (receiveResult.MessageType != _webSocketMessageType)
                    {
                        CloseStatus = WebSocketCloseStatus.InvalidMessageType;
                        CloseStatusDescription = "An unsupported message type was received";
                        throw new InvalidOperationException(CloseStatusDescription);
                    }

                    if (_jsonBuffer.Buffer.Length < receiveResult.Count + _jsonBuffer.BufferCurPos)
                    {
                        _jsonBuffer.IncreaseBuffer();
                    }

                    Buffer.BlockCopy(buffer.Array, 0, _jsonBuffer.Buffer, _jsonBuffer.BufferCurPos, receiveResult.Count);
                    _arrayPool.Return(buffer.Array, true);

                    _jsonBuffer.BufferCurPos += receiveResult.Count;
                    if (receiveResult.EndOfMessage) break;
                }

                byte[] jsonBytes;
                if (!_jsonBuffer.TryExtractJsonFromBuffer(out jsonBytes))
                {
                    // A no JSON message was received
                    return null;
                }

                var envelopeJson = Encoding.UTF8.GetString(jsonBytes);

                if (_traceWriter != null &&
                    _traceWriter.IsEnabled)
                {
                    await _traceWriter.TraceAsync(envelopeJson, DataOperation.Receive).ConfigureAwait(false);
                }
                return _envelopeSerializer.Deserialize(envelopeJson);
            }
            finally
            {
                _receiveSemaphore.Release();
            }
        }

        /// <summary>
        /// Closes the transport, implementing the websocket close handshake.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">The listener is not active</exception>
        protected override async Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            // Awaits for the client to send the close connection frame.
            // If the session was clearly closed, the client should received the finished envelope and is closing the connection.
            using (var cts = new CancellationTokenSource(CloseTimeout))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token))
            {
                if (WebSocket.State == WebSocketState.Open)
                {
                    await
                        WebSocket.CloseAsync(CloseStatus, CloseStatusDescription, cancellationToken)
                            .ConfigureAwait(false); 
                }
                else if (WebSocket.State == WebSocketState.CloseReceived)
                {
                    await
                        WebSocket.CloseOutputAsync(CloseStatus, CloseStatusDescription, cancellationToken)
                            .ConfigureAwait(false);
                }
            }
        }

        private async Task HandleCloseMessageAsync(WebSocketReceiveResult receiveResult, CancellationToken cancellationToken)
        {
            await
                WebSocket.CloseOutputAsync(CloseStatus, CloseStatusDescription, cancellationToken)
                    .ConfigureAwait(false);

            CloseStatus = WebSocketCloseStatus.NormalClosure;
        }

        public override bool IsConnected => WebSocket.State
            >= WebSocketState.Open && WebSocket.State <= WebSocketState.CloseReceived; // We need to consider the Close status here to make the channel call the CloseAsync method.


        public override IReadOnlyDictionary<string, object> Options => new Dictionary<string, object>()
        {
            {nameof(System.Net.WebSockets.WebSocket.SubProtocol), WebSocket.SubProtocol},
            {nameof(System.Net.WebSockets.WebSocket.CloseStatusDescription), WebSocket.CloseStatusDescription},
            {nameof(System.Net.WebSockets.WebSocket.CloseStatus), WebSocket.CloseStatus},
            {nameof(System.Net.WebSockets.WebSocket.DefaultKeepAliveInterval), System.Net.WebSockets.WebSocket.DefaultKeepAliveInterval}
        };

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                WebSocket.Dispose();
                _sendSemaphore.Dispose();
            }
        }

        private Task CloseWithTimeoutAsync()
        {
            using (var cts = new CancellationTokenSource(CloseTimeout))
            {
                return CloseAsync(cts.Token);
            }
        }
    }
}
