using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;

namespace Lime.Transport.WebSocket
{
    public abstract class WebSocketTransport : TransportBase, IDisposable
    {
        public const int DEFAULT_BUFFER_SIZE = 8192;
        public const int DEFAULT_MAX_BUFFER_COUNT = 1024;

        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);

        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly ArrayPool<byte> _arrayPool;
        private readonly int _bufferSize;
        private readonly SemaphoreSlim _sendSemaphore;
        private readonly SemaphoreSlim _receiveSemaphore;
        private readonly SemaphoreSlim _closeSemaphore;
        private readonly WebSocketMessageType _webSocketMessageType;
        private readonly CancellationTokenSource _receiveCts;

        private WebSocketReceiveResult _closeFrame;
        protected WebSocketCloseStatus CloseStatus;
        protected string CloseStatusDescription;

        protected WebSocketTransport(
            System.Net.WebSockets.WebSocket webSocket,
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null,
            int bufferSize = DEFAULT_BUFFER_SIZE,
            WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text,
            ArrayPool<byte> arrayPool = null)
        {
            WebSocket = webSocket;
            _envelopeSerializer = envelopeSerializer;
            _traceWriter = traceWriter;
            _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;
            _bufferSize = bufferSize;
            _webSocketMessageType = webSocketMessageType;
            _sendSemaphore = new SemaphoreSlim(1);
            _receiveSemaphore = new SemaphoreSlim(1);
            _closeSemaphore = new SemaphoreSlim(1);
            _receiveCts = new CancellationTokenSource();
            CloseStatus = WebSocketCloseStatus.NormalClosure;
            CloseStatusDescription = string.Empty;
        }

        protected System.Net.WebSockets.WebSocket WebSocket { get; }

        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            EnsureOpen("send");

            var serializedEnvelope = _envelopeSerializer.Serialize(envelope);

            if (_traceWriter != null &&
                _traceWriter.IsEnabled)
            {
                await _traceWriter.TraceAsync(serializedEnvelope, DataOperation.Send).ConfigureAwait(false);
            }

            var buffer = Encoding.UTF8.GetBytes(serializedEnvelope);


            await _sendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                EnsureOpen("send");

                await WebSocket.SendAsync(new ArraySegment<byte>(buffer), _webSocketMessageType, true,
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
            EnsureOpen("receive");

            var segments = new List<BufferSegment>();

            await _receiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                EnsureOpen("receive");

                while (!_receiveCts.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var segment = new BufferSegment
                    {
                        Buffer = _arrayPool.Rent(_bufferSize)
                    };
                    segments.Add(segment);

                    // The websocket class go to the 'Aborted' state if the receiveasync operation is cancelled.
                    // In this case, we are unable to close the connection clearly when required.
                    var receiveTask = WebSocket.ReceiveAsync(new ArraySegment<byte>(segment.Buffer), _receiveCts.Token);
                    var cancellationTask = cancellationToken.AsTask();

                    // If the token is cancelled
                    var completedTask = await Task.WhenAny(receiveTask, cancellationTask).ConfigureAwait(false);
                    if (completedTask != receiveTask)
                    {
                        // The task above will thrown a TaskCancelledException, but just in case...
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    var receiveResult = await receiveTask;
                    if (receiveResult == null) continue;

                    segment.Count = receiveResult.Count;

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        HandleCloseMessage(receiveResult);
                        break;
                    }

                    if (receiveResult.MessageType != _webSocketMessageType)
                    {
                        CloseStatus = WebSocketCloseStatus.InvalidMessageType;
                        CloseStatusDescription = "An unsupported message type was received";
                        throw new InvalidOperationException(CloseStatusDescription);
                    }

                    if (receiveResult.EndOfMessage) break;

                    if (segments.Count + 1 > DEFAULT_MAX_BUFFER_COUNT)
                    {
                        throw new BufferOverflowException("Maximum buffer size reached");
                    }
                }
            }
            catch (WebSocketException)
            {
                foreach (var segment in segments)
                {
                    _arrayPool.Return(segment.Buffer);
                }

                await CloseWithTimeoutAsync().ConfigureAwait(false);
                throw;
            }
            catch (Exception)
            {
                foreach (var segment in segments)
                {
                    _arrayPool.Return(segment.Buffer);
                }

                throw;
            }
            finally
            {
                _receiveSemaphore.Release();
            }

            string serializedEnvelope = null;
            // Build the serialized envelope using the buffers
            using (var stream = new MemoryStream())
            {
                foreach (var segment in segments)
                {
                    stream.Write(segment.Buffer, 0, segment.Count);
                    _arrayPool.Return(segment.Buffer);
                }

                var buffer = stream.ToArray();
                serializedEnvelope = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            }

            if (_traceWriter != null &&
                _traceWriter.IsEnabled)
            {
                await _traceWriter.TraceAsync(serializedEnvelope, DataOperation.Receive).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(serializedEnvelope)) return null;

            return _envelopeSerializer.Deserialize(serializedEnvelope);
        }

        /// <summary>
        /// Closes the transport, implementing the websocket close handshake.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">The listener is not active</exception>
        protected override async Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            await _closeSemaphore.WaitAsync(cancellationToken);
            try
            {
                // Awaits for the client to send the close connection frame.
                // If the session was clearly closed, the client should received the finished envelope and is closing the connection.
                using (var cts = new CancellationTokenSource(CloseTimeout))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token))
                {
                    if (WebSocket.State == WebSocketState.Open ||
                        WebSocket.State == WebSocketState.CloseReceived)
                    {
                        await
                            WebSocket.CloseAsync(CloseStatus, CloseStatusDescription, linkedCts.Token)
                                .ConfigureAwait(false);
                    }
                }

                if (!_receiveCts.IsCancellationRequested)
                {
                    _receiveCts.Cancel();
                }
            }
            finally
            {
                _closeSemaphore.Release();
            }
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
                _receiveSemaphore.Dispose();
                _closeSemaphore.Dispose();
                _receiveCts.Dispose();
            }
        }

        private void EnsureOpen(string operation)
        {
            if (WebSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException($"Cannot {operation} in the websocket connection state '{WebSocket.State}'");
            }
        }

        private void HandleCloseMessage(WebSocketReceiveResult receiveResult)
        {
            CloseStatus = WebSocketCloseStatus.NormalClosure;
            _closeFrame = receiveResult;
        }

        private Task CloseWithTimeoutAsync()
        {
            using (var cts = new CancellationTokenSource(CloseTimeout))
            {
                return CloseAsync(cts.Token);
            }
        }

        private class BufferSegment
        {
            public byte[] Buffer;

            public int Count;

            public int Remaining => Buffer.Length - Count;
        }
    }
}
