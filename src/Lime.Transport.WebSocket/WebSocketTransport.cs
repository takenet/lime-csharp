using System;
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
        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);

        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;        
        private readonly JsonBuffer _jsonBuffer;
        private readonly SemaphoreSlim _receiveSemaphore;
        private readonly SemaphoreSlim _sendSemaphore;

        private readonly BufferBlock<Envelope> _receivedEnvelopeBufferBlock;        
        private CancellationTokenSource _listenerCts;
        private Task _listenerTask;
        private TaskCompletionSource<WebSocketReceiveResult> _closeFrameTcs;

        protected WebSocketCloseStatus CloseStatus;
        protected string CloseStatusDescription;

        protected WebSocketTransport(
            System.Net.WebSockets.WebSocket webSocket,
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null,
            int bufferSize = 8192, 
            int receiveBoundedCapacity = 5)
        {            
            WebSocket = webSocket;
            _envelopeSerializer = envelopeSerializer;
            _traceWriter = traceWriter;            
            _jsonBuffer = new JsonBuffer(bufferSize);
            _receiveSemaphore = new SemaphoreSlim(1);
            _sendSemaphore = new SemaphoreSlim(1);
            CloseStatus = WebSocketCloseStatus.NormalClosure;
            CloseStatusDescription = string.Empty;
            _receivedEnvelopeBufferBlock = new BufferBlock<Envelope>(
                new DataflowBlockOptions() { BoundedCapacity = receiveBoundedCapacity });
        }

        protected System.Net.WebSockets.WebSocket WebSocket { get; }

        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (WebSocket.State != WebSocketState.Open)
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
                await WebSocket.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, true,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (WebSocket.State != WebSocketState.Open || _listenerTask == null)
            {
                throw new InvalidOperationException("The connection was not initialized. Call OpenAsync first.");
            }

            if (_listenerTask.IsCompleted)
            {
                // Awaits the listener task to throw any exception to the caller
                await _listenerTask.WithCancellation(cancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException("The listener task is finished");
            }

            return await _receivedEnvelopeBufferBlock.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var buffer = new ArraySegment<byte>(_jsonBuffer.Buffer);
                    while (true)
                    {
                        var receiveResult = await WebSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            await HandleCloseMessageAsync(receiveResult);
                            return;
                        }

                        if (receiveResult.MessageType != WebSocketMessageType.Text)
                        {
                            CloseStatus = WebSocketCloseStatus.InvalidMessageType;
                            CloseStatusDescription = "An unsupported message type was received";
                            throw new InvalidOperationException(CloseStatusDescription);
                        }

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
                        var envelope = _envelopeSerializer.Deserialize(envelopeJson);
                        await _receivedEnvelopeBufferBlock.SendAsync(envelope, cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }   
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (_listenerTask != null) throw new InvalidOperationException("The listener is already active");
            _listenerCts?.Dispose();
            _listenerCts = new CancellationTokenSource();
            _closeFrameTcs = new TaskCompletionSource<WebSocketReceiveResult>();
            _listenerTask = Task.Run(() => ListenAsync(_listenerCts.Token));
            return Task.CompletedTask;
        }

        protected override async Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            if (_listenerTask == null) throw new InvalidOperationException("The listener is not active");
            try
            {
                if (WebSocket.State == WebSocketState.Open)
                {
                    using (var cts = new CancellationTokenSource(CloseTimeout))
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token))
                    {
                        try
                        {
                            // Awaits for the other party to send the close connection frame
                            using (linkedCts.Token.Register(() => _closeFrameTcs.TrySetCanceled()))
                            {
                                await _closeFrameTcs.Task.ConfigureAwait(false);
                            }
                        }
                        catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
                        await CloseWebSocketOutputAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    await CloseWebSocketOutputAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                StopListenerTask();
            }
        }

        protected void StopListenerTask()
        {
            _listenerCts?.Cancel();
        }

        private async Task CloseWebSocketOutputAsync(CancellationToken cancellationToken)
        {
            if (WebSocket.State == WebSocketState.Open ||
                WebSocket.State == WebSocketState.CloseReceived)
            {
                await
                    WebSocket.CloseOutputAsync(CloseStatus, CloseStatusDescription, cancellationToken)
                        .ConfigureAwait(false);
            }
        }

        public override bool IsConnected => WebSocket.State == WebSocketState.Open;

        public override IReadOnlyDictionary<string, object> Options => new Dictionary<string, object>()
        {
            {nameof(System.Net.WebSockets.WebSocket.SubProtocol), WebSocket.SubProtocol},
            {nameof(System.Net.WebSockets.WebSocket.CloseStatusDescription), WebSocket.CloseStatusDescription},
            {nameof(System.Net.WebSockets.WebSocket.CloseStatus), WebSocket.CloseStatus},
            {nameof(System.Net.WebSockets.WebSocket.DefaultKeepAliveInterval), System.Net.WebSockets.WebSocket.DefaultKeepAliveInterval}
        };


        private async Task HandleCloseMessageAsync(WebSocketReceiveResult receiveResult)
        {
            if (_traceWriter != null &&
                _traceWriter.IsEnabled &&
                receiveResult.CloseStatus != null &&
                receiveResult.CloseStatus.Value != WebSocketCloseStatus.Empty &&
                receiveResult.CloseStatus.Value != WebSocketCloseStatus.NormalClosure)
            {
                await
                    _traceWriter.TraceAsync(
                        $"{receiveResult.CloseStatus.Value}: {receiveResult.CloseStatusDescription}",
                        DataOperation.Error);
            }

            CloseStatus = WebSocketCloseStatus.NormalClosure;
           _closeFrameTcs.TrySetResult(receiveResult);
        }

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
                _listenerCts?.Dispose();
            }
        }
    }
}
