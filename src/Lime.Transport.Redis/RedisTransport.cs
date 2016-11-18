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
            IConnectionMultiplexer connectionMultiplexer, 
            IEnvelopeSerializer envelopeSerializer, 
            ITraceWriter traceWriter = null)
            : this(connectionMultiplexer, envelopeSerializer, traceWriter, SERVER_CHANNEL_PREFIX, CLIENT_CHANNEL_PREFIX)
        {
            
        }

        internal RedisTransport(
            IConnectionMultiplexer connectionMultiplexer, 
            IEnvelopeSerializer envelopeSerializer, 
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
            var subscriber = _connectionMultiplexer.GetSubscriber();

            var session = envelope as Session;
            if (session != null &&
                session.State <= SessionState.Established)
            {
                if (string.IsNullOrWhiteSpace(session.Id))
                {
                    if (session.State != SessionState.New)
                    {
                        throw new InvalidOperationException("The session envelope must have an id");
                    }

                    if (_sendChannelName != null)
                    {
                        throw new InvalidOperationException("The send channel name is already defined");
                    }
                    
                    // Create a temporary channel for receiving the session data.
                    session.Id = EnvelopeId.NewId();                    
                }

                var receiveChannelName = GetReceiveChannelName(session.Id);

                if (_receiveChannelName == null ||
                    !_receiveChannelName.Equals(receiveChannelName))
                {
                    if (_receiveChannelName != null)
                    {
                        await subscriber
                            .UnsubscribeAsync(_receiveChannelName)
                            .WithCancellation(cancellationToken)
                            .ConfigureAwait(false);
                    }

                    _receiveChannelName = receiveChannelName;

                    await subscriber
                        .SubscribeAsync(_receiveChannelName, HandleReceivedData)
                        .WithCancellation(cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            var envelopeJson = _envelopeSerializer.Serialize(envelope);

            await _traceWriter.TraceIfEnabledAsync(envelopeJson, DataOperation.Send);

            // Send to the channel or to the server prefix
            await subscriber
                .PublishAsync(_sendChannelName ?? SERVER_CHANNEL_PREFIX, envelopeJson)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false);
        }

        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            var envelope = await ReceivedEnvelopesBufferBlock.ReceiveAsync(cancellationToken).ConfigureAwait(false);

            var session = envelope as Session;
            if (session != null &&
                session.State <= SessionState.Established)
            {
                if (string.IsNullOrWhiteSpace(session.Id))
                {
                    throw new InvalidOperationException("An invalid session envelope was received: The session envelope must have an id");
                }

                _sendChannelName = GetSendChannelName(session.Id);
                if (session.State == SessionState.New) session.Id = null;
            }

            return envelope;
        }

        public override bool IsConnected => true;

        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            if (_receiveChannelName == null) return Task.CompletedTask;

            return _connectionMultiplexer
                .GetSubscriber()
                .UnsubscribeAsync(_receiveChannelName).WithCancellation(cancellationToken);
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void HandleReceivedData(RedisChannel channel, RedisValue value)
        {
            var envelopeJson = (string) value;

            _traceWriter.TraceIfEnabledAsync(envelopeJson, DataOperation.Receive).Wait();
            var envelope = _envelopeSerializer.Deserialize(envelopeJson);
            if (!ReceivedEnvelopesBufferBlock.Post(envelope))
            {                
                 _traceWriter.TraceIfEnabledAsync("RedisTransport: The input buffer has not accepted the envelope", DataOperation.Error).Wait();
            }
        }
        
        private string GetReceiveChannelName(string id) => $"{_receiveChannelPrefix}:{id}";

        private string GetSendChannelName(string id) => $"{_sendChannelPrefix}:{id}";

    }
}
