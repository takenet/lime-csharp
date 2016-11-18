using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using StackExchange.Redis;

namespace Lime.Transport.Redis
{
    public class RedisTransport : TransportBase
    {
        private const string REPLY_CHANNEL_KEY = "lime.transport.redis.replyChannel";

        internal const string CLIENT_CHANNEL_PREFIX = "client";
        internal const string SERVER_CHANNEL_PREFIX = "server";

        private readonly IConnectionMultiplexer _connectionMultiplexer;        
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly string _sendChannelPrefix;
        private readonly string _receiveChannelPrefix;

        internal readonly BufferBlock<Envelope> ReceivedEnvelopesBufferBlock;
       
        private string _sendChannelName;
        private string _receiveChannelName;

        public RedisTransport(
            IEnvelopeSerializer envelopeSerializer,
            IConnectionMultiplexer connectionMultiplexer,
            ITraceWriter traceWriter = null)
            : this(envelopeSerializer, connectionMultiplexer, traceWriter, SERVER_CHANNEL_PREFIX, CLIENT_CHANNEL_PREFIX)
        {
            
        }

        internal RedisTransport(
            IEnvelopeSerializer envelopeSerializer,
            IConnectionMultiplexer connectionMultiplexer,                         
            ITraceWriter traceWriter,
            string sendChannelPrefix,
            string receiveChannelPrefix)
        {
            if (connectionMultiplexer == null) throw new ArgumentNullException(nameof(connectionMultiplexer));
            _connectionMultiplexer = connectionMultiplexer;            
            _traceWriter = traceWriter;
            _sendChannelPrefix = sendChannelPrefix;
            _receiveChannelPrefix = receiveChannelPrefix;
            _envelopeSerializer = envelopeSerializer;
            ReceivedEnvelopesBufferBlock = new BufferBlock<Envelope>(
                new DataflowBlockOptions() { BoundedCapacity = DataflowBlockOptions.Unbounded});
        }

        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (_receiveChannelName == null)
            {
                var session = envelope as Session;
                if (session == null) throw new InvalidOperationException("A session envelope was expected");

                if (string.IsNullOrWhiteSpace(session.Id))
                {
                    _receiveChannelName = GetReceiveChannelName(session.Id);
                }

            }

            var envelopeJson = _envelopeSerializer.Serialize(envelope);

            await _connectionMultiplexer
                .GetSubscriber()
                .PublishAsync(_sendChannelName ?? _sendChannelPrefix, envelopeJson)
                .ConfigureAwait(false);
        }

        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            var envelope = await ReceivedEnvelopesBufferBlock.ReceiveAsync(cancellationToken).ConfigureAwait(false);

            if (envelope?.Metadata != null && 
                envelope.Metadata.ContainsKey(REPLY_CHANNEL_KEY))
            {
                _sendChannelName = envelope.Metadata[REPLY_CHANNEL_KEY];
                envelope.Metadata.Remove(REPLY_CHANNEL_KEY);
                if (envelope.Metadata.Count == 0) envelope.Metadata = null;
            }

            return envelope;
        }

        public override bool IsConnected => true;

        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            return _connectionMultiplexer.GetSubscriber().UnsubscribeAllAsync().WithCancellation(cancellationToken);
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;

        }

        private async void HandleReceivedData(RedisChannel channel, RedisValue value)
        {
            var envelopeJson = (string) value;

            await _traceWriter.TraceIfEnabledAsync(envelopeJson, DataOperation.Receive);
            var envelope = _envelopeSerializer.Deserialize(envelopeJson);
            if (!ReceivedEnvelopesBufferBlock.Post(envelope))
            {                
                await _traceWriter.TraceIfEnabledAsync("RedisTransport: The input buffer has not accepted the envelope", DataOperation.Error);
            }
        }



        private string GetReceiveChannelName(string id) => $"{_receiveChannelPrefix}:{id}";

        private string GetSendChannelName(string id) => $"{_sendChannelName}:{id}";

    }
}
