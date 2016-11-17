using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using StackExchange.Redis;

namespace Lime.Transport.Redis
{
    public class RedisTransport : TransportBase
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly RedisChannel _sendChannel;
        private readonly RedisChannel _receiveChannel;
        private readonly IEnvelopeSerializer _envelopeSerializer;

        public RedisTransport(
            IEnvelopeSerializer envelopeSerializer,
            IConnectionMultiplexer connectionMultiplexer, 
            RedisChannel sendChannel,
            RedisChannel receiveChannel)
        {
            if (connectionMultiplexer == null) throw new ArgumentNullException(nameof(connectionMultiplexer));
            _connectionMultiplexer = connectionMultiplexer;
            _sendChannel = sendChannel;
            _receiveChannel = receiveChannel;
            _envelopeSerializer = envelopeSerializer;
        }

        public override Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            var envelopeJson = _envelopeSerializer.Serialize(envelope);

            return _connectionMultiplexer
                .GetSubscriber()
                .PublishAsync(_sendChannel, envelopeJson);
        }

        public override Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override bool IsConnected => _connectionMultiplexer.IsConnected;

        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {            
            throw new NotImplementedException();
        }
    }
}
