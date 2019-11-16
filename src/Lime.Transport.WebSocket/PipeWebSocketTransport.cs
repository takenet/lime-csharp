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
using ReflectionMagic;

namespace Lime.Transport.WebSocket
{
    public abstract class PipeWebSocketTransport : TransportBase, IDisposable
    {
        public const int DEFAULT_BUFFER_SIZE = 8192;
        public const int DEFAULT_MAX_BUFFER_COUNT = 1024;

        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);
        
        private readonly SemaphoreSlim _closeSemaphore;
        private readonly WebSocketMessageType _webSocketMessageType;
        private readonly bool _closeGracefully;
        private readonly CancellationTokenSource _receiveCts;
        private readonly EnvelopePipe _envelopePipe;
        
        protected WebSocketCloseStatus CloseStatus;
        protected string CloseStatusDescription;

        protected PipeWebSocketTransport(
            System.Net.WebSockets.WebSocket webSocket,
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null,
            int bufferSize = DEFAULT_BUFFER_SIZE,
            WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text,
            ArrayPool<byte> arrayPool = null,
            bool closeGracefully = true)
        {
            WebSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _webSocketMessageType = webSocketMessageType;
            _closeGracefully = closeGracefully;
            _closeSemaphore = new SemaphoreSlim(1);
            _receiveCts = new CancellationTokenSource();
            CloseStatus = WebSocketCloseStatus.NormalClosure;
            CloseStatusDescription = string.Empty;
            _envelopePipe = new EnvelopePipe(ReceiveFromPipeAsync, SendToPipeAsync, envelopeSerializer, traceWriter);
        }

        private ValueTask SendToPipeAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            return WebSocket.SendAsync(buffer, _webSocketMessageType, true, cancellationToken);
        }

        private async ValueTask<int> ReceiveFromPipeAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var receiveResult = await WebSocket.ReceiveAsync(buffer, cancellationToken);
            
            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                HandleCloseMessage(receiveResult);
                return 0;
            }

            if (receiveResult.MessageType != _webSocketMessageType)
            {
                CloseStatus = WebSocketCloseStatus.InvalidMessageType;
                CloseStatusDescription = "An unsupported message type was received";
                throw new InvalidOperationException(CloseStatusDescription);
            }
            
            return receiveResult.Count;
        }

        protected System.Net.WebSockets.WebSocket WebSocket { get; }

        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            EnsureOpen("send");

            try
            {
                EnsureOpen("send");
                await _envelopePipe.SendAsync(envelope, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                await CloseWithTimeoutAsync().ConfigureAwait(false);
                throw;
            }

        }

        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            EnsureOpen("receive");

            try
            {
                return await _envelopePipe.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                await CloseWithTimeoutAsync().ConfigureAwait(false);
                throw;
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

        public override bool IsConnected => WebSocket.State == WebSocketState.Open ||
                                            WebSocket.State == WebSocketState.CloseReceived; // We need to consider the CloseReceived status here to make the channel call the CloseAsync method.

        public override string LocalEndPoint
        {
            get
            {
                try
                {
                    try
                    {
                        return WebSocket.AsDynamic()._innerStream?._context?.Request?.LocalEndPoint?.ToString();
                    }
                    catch
                    {
                        return WebSocket.AsDynamic()._stream?.Socket?.LocalEndPoint?.ToString();
                    }
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
                    try
                    {
                        return WebSocket.AsDynamic()._innerStream?._context?.Request?.RemoteEndPoint?.ToString();
                    }
                    catch
                    {
                        return WebSocket.AsDynamic()._stream?.Socket?.RemoteEndPoint?.ToString();
                    }
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

        private void HandleCloseMessage(ValueWebSocketReceiveResult receiveResult)
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