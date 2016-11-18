using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using StackExchange.Redis;

namespace Lime.Transport.Redis
{
    public class RedisTransportListener : ITransportListener
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;

        private readonly BufferBlock<ITransport> _transportBufferBlock;

        public RedisTransportListener(
            IEnvelopeSerializer envelopeSerializer,
            IConnectionMultiplexer connectionMultiplexer,
            ITraceWriter traceWriter = null,
            int acceptTransportBoundedCapacity = 10)
        {
            if (connectionMultiplexer == null) throw new ArgumentNullException(nameof(connectionMultiplexer));
            _envelopeSerializer = envelopeSerializer;
            _connectionMultiplexer = connectionMultiplexer;
            _traceWriter = traceWriter;

            _transportBufferBlock = new BufferBlock<ITransport>(
                new DataflowBlockOptions() { BoundedCapacity = acceptTransportBoundedCapacity });
        }

        public Uri[] ListenerUris
            => _connectionMultiplexer.GetEndPoints().Select(e => new Uri($"redis://{e}")).ToArray();

        public Task StartAsync()
        {
            return _connectionMultiplexer
                .GetSubscriber()
                .SubscribeAsync(RedisTransport.SERVER_CHANNEL_PREFIX, HandleReceivedData);            
        }

        public Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync()
        {
            return _connectionMultiplexer
                .GetSubscriber()
                .UnsubscribeAllAsync();
        }

        private async void HandleReceivedData(RedisChannel channel, RedisValue value)
        {
            var envelopeJson = (string)value;
            await _traceWriter.TraceIfEnabledAsync(envelopeJson, DataOperation.Receive);

            var envelope = _envelopeSerializer.Deserialize(envelopeJson);
            var session = envelope as Session;
            if (session == null ||
                session.State != SessionState.New)
            {
                await _traceWriter.TraceAsync("RedisTransportListener: An unexpected envelope was received",
                    DataOperation.Error);
            }
            else
            {
                var transport = new RedisTransport(_envelopeSerializer, _connectionMultiplexer, _traceWriter,
                    RedisTransport.CLIENT_CHANNEL_PREFIX, RedisTransport.SERVER_CHANNEL_PREFIX);
                await _transportBufferBlock.SendAsync(transport);
                await transport.ReceivedEnvelopesBufferBlock.SendAsync(envelope);
            }
        }
    }
}
