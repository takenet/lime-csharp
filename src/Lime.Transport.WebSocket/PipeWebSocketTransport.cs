using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.WebSockets;
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
        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);
        
        private readonly SemaphoreSlim _closeSemaphore;
        private readonly WebSocketMessageType _webSocketMessageType;
        private readonly bool _closeGracefully;
        private readonly EnvelopePipe _envelopePipe;
        
        protected WebSocketCloseStatus CloseStatus;
        protected string CloseStatusDescription;

        protected PipeWebSocketTransport(
            System.Net.WebSockets.WebSocket webSocket,
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null,
            WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text,
            bool closeGracefully = true,
            int pauseWriterThreshold = EnvelopePipe.DEFAULT_PAUSE_WRITER_THRESHOLD,
            MemoryPool<byte> memoryPool = null)
        {
            WebSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _webSocketMessageType = webSocketMessageType;
            _closeGracefully = closeGracefully;
            _closeSemaphore = new SemaphoreSlim(1);
            CloseStatus = WebSocketCloseStatus.NormalClosure;
            CloseStatusDescription = string.Empty;
            _envelopePipe = new EnvelopePipe(ReceiveFromPipeAsync, SendToPipeAsync, envelopeSerializer, traceWriter, pauseWriterThreshold, memoryPool);
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

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            return _envelopePipe.StartAsync(cancellationToken);
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
                await _envelopePipe.StopAsync(cancellationToken);
                
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
                _envelopePipe.Dispose();
                _closeSemaphore.Dispose();
            }
        }

        private void EnsureOpen(string operation)
        {
            if (WebSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException($"Cannot {operation} in the websocket connection state '{WebSocket.State}'");
            }
        }
        
        
        private ValueTask SendToPipeAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) 
            => WebSocket.SendAsync(buffer, _webSocketMessageType, true, cancellationToken);

        private async ValueTask<int> ReceiveFromPipeAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            // The WebSocket ReceiveAsync method doesn't support cancellation 
            var receiveTask = WebSocket.ReceiveAsync(buffer, cancellationToken).AsTask();
            var cancellationTask = cancellationToken.AsTask();

            var completedTask = await Task.WhenAny(receiveTask, cancellationTask).ConfigureAwait(false);
            if (completedTask != receiveTask)
            {
                // The task above will thrown a TaskCancelledException, but just in case...
                cancellationToken.ThrowIfCancellationRequested();
            }
            
            var receiveResult = await receiveTask;
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
    }
}