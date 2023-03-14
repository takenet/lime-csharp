using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Tracing;
using ReflectionMagic;

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
        private readonly SemaphoreSlim _closeSemaphore;
        private readonly WebSocketMessageType _webSocketMessageType;
        private readonly bool _closeGracefully;
        private readonly CancellationTokenSource _sendReceiveCts;

        protected WebSocketCloseStatus CloseStatus;
        protected string CloseStatusDescription;

        protected WebSocketTransport(
            System.Net.WebSockets.WebSocket webSocket,
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null,
            int bufferSize = DEFAULT_BUFFER_SIZE,
            WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text,
            ArrayPool<byte> arrayPool = null,
            bool closeGracefully = true)
        {
            WebSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _envelopeSerializer = envelopeSerializer;
            _traceWriter = traceWriter;
            _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;
            _bufferSize = bufferSize;
            _webSocketMessageType = webSocketMessageType;
            _closeGracefully = closeGracefully;
            _closeSemaphore = new SemaphoreSlim(1);
            _sendReceiveCts = new CancellationTokenSource();
            CloseStatus = WebSocketCloseStatus.NormalClosure;
            CloseStatusDescription = string.Empty;
        }

        protected System.Net.WebSockets.WebSocket WebSocket { get; }

        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            using var activity = envelope.StartActivity(
                $"WebSocketTransport.Send {envelope.GetActivityName()}",
                ActivityKind.Client,
                ignoreCurrentActivity: true,
                activitySource: LimeWebSocketActivitySource.Instance
            );
            activity?.SetTransportTags(this);

            EnsureOpen("send");

            var serializedEnvelope = _envelopeSerializer.Serialize(envelope);

            if (_traceWriter != null &&
                _traceWriter.IsEnabled)
            {
                await _traceWriter.TraceAsync(serializedEnvelope, DataOperation.Send).ConfigureAwait(false);
            }

            var buffer = _arrayPool.Rent(Encoding.UTF8.GetByteCount(serializedEnvelope));
            var length = Encoding.UTF8.GetBytes(serializedEnvelope, 0, serializedEnvelope.Length, buffer, 0);

            try
            {
                EnsureOpen("send");

                // The WebSocket class doesn't support cancellations on Send/Receive operations...
                await WebSocket
                    .SendAsync(
                        new ArraySegment<byte>(buffer, 0, length),
                        _webSocketMessageType,
                        true,
                        _sendReceiveCts.Token)
                    .WithCancellation(cancellationToken);
            }
            catch (WebSocketException)
            {
                await CloseWithTimeoutAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                _arrayPool.Return(buffer);
            }
        }

        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            EnsureOpen("receive");

            var segments = new List<BufferSegment>();

            try
            {
                EnsureOpen("receive");

                while (!_sendReceiveCts.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var segment = new BufferSegment
                    {
                        Buffer = _arrayPool.Rent(_bufferSize)
                    };
                    segments.Add(segment);

                    // The websocket class go to the 'Aborted' state if the ReceiveAsync operation is cancelled.
                    // In this case, we are unable to close the connection clearly when required.
                    // So, we must use a different cancellation token.
                    var receiveResult = await WebSocket
                        .ReceiveAsync(new ArraySegment<byte>(segment.Buffer), _sendReceiveCts.Token)
                        .WithCancellation(cancellationToken);

                    if (receiveResult == null)
                    {
                        continue;
                    }

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

                    if (receiveResult.EndOfMessage)
                    {
                        break;
                    }

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

            if (string.IsNullOrWhiteSpace(serializedEnvelope))
            {
                return null;
            }

            var envelope = _envelopeSerializer.Deserialize(serializedEnvelope);

            if (envelope == null)
            {
                return null;
            }

            using var activity = envelope.StartActivity(
                $"WebSocketTransport.Receive {envelope.GetType().Name}",
                ActivityKind.Server,
                ignoreCurrentActivity: true,
                activitySource: LimeWebSocketActivitySource.Instance
            );
            activity?.SetTransportTags(this);

            return envelope;
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
                        if (_closeGracefully)
                        {
                            await
                                WebSocket.CloseAsync(CloseStatus, CloseStatusDescription, linkedCts.Token)
                                    .ConfigureAwait(false);
                        }
                        else
                        {
                            await
                                WebSocket.CloseOutputAsync(CloseStatus, CloseStatusDescription, linkedCts.Token)
                                    .ConfigureAwait(false);
                        }
                    }
                }

                _sendReceiveCts.CancelIfNotRequested();
            }
            finally
            {
                _closeSemaphore.Release();
            }
        }

        public override bool IsConnected => WebSocket.State == WebSocketState.Open ||
                                            WebSocket.State == WebSocketState.CloseReceived; // We need to consider the CloseReceived status here to make the channel call the CloseAsync method.

        public override string LocalEndPoint
        {
            get
            {
                try
                {
                    return WebSocket.AsDynamic()._innerStream?._context?.Request?.LocalEndPoint?.ToString();
                }
                catch
                {
                    return base.LocalEndPoint;
                }
            }
        }

        public override string RemoteEndPoint
        {
            get
            {
                try
                {
                    return WebSocket.AsDynamic()._innerStream?._context?.Request?.RemoteEndPoint?.ToString();
                }
                catch
                {
                    return base.RemoteEndPoint;
                }
            }
        }

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
                _closeSemaphore.Dispose();
                _sendReceiveCts.Dispose();
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