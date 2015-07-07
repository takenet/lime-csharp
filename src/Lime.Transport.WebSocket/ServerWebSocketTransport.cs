using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Util;
using vtortola.WebSockets;

namespace Lime.Transport.WebSocket
{
    public sealed class ServerWebSocketTransport : TransportBase, IDisposable
    {
        private readonly vtortola.WebSockets.WebSocket _webSocket;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;

        internal ServerWebSocketTransport(vtortola.WebSockets.WebSocket webSocket, IEnvelopeSerializer envelopeSerializer, ITraceWriter traceWriter = null)
        {
            if (webSocket == null) throw new ArgumentNullException("webSocket");
            _webSocket = webSocket;
            _envelopeSerializer = envelopeSerializer;
            _traceWriter = traceWriter;
        }

        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            using (var stream = _webSocket.CreateMessageWriter(WebSocketMessageType.Text))
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    var envelopeJson = _envelopeSerializer.Serialize(envelope);
                    await TraceDataIfEnabledAsync(envelopeJson, DataOperation.Send).ConfigureAwait(false);
                    await writer.WriteAsync(envelopeJson).ConfigureAwait(false);                    
                }
            }
        }

        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            using (var stream = await _webSocket.ReadMessageAsync(cancellationToken).ConfigureAwait(false))
            {
                if (stream.MessageType == WebSocketMessageType.Binary)
                {
                    throw new NotSupportedException("An unsupported binary message was received");
                }
                
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    var envelopeJson = await reader.ReadToEndAsync().ConfigureAwait(false);
                    await TraceDataIfEnabledAsync(envelopeJson, DataOperation.Receive).ConfigureAwait(false);
                    return _envelopeSerializer.Deserialize(envelopeJson);
                }
            }
        }

        public override Task OpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (!_webSocket.IsConnected)
            {
                throw new InvalidOperationException("The transport is not connected");
            }

            return TaskUtil.CompletedTask;
        }

        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            _webSocket.Close();
            return TaskUtil.CompletedTask;
        }

        private async Task TraceDataIfEnabledAsync(string envelopeJson, DataOperation dataOperation)
        {
            if (_traceWriter != null &&
                _traceWriter.IsEnabled)
            {
                await _traceWriter.TraceAsync(envelopeJson, dataOperation).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _webSocket.Dispose();
        }
    }
}
