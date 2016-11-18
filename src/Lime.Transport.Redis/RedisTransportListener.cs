using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.Server;
using StackExchange.Redis;

namespace Lime.Transport.Redis
{
    public sealed class RedisTransportListener : ITransportListener, IDisposable
    {
        public static readonly string RedisScheme = "redis";

        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly string _channelNamespace;
        private readonly IConnectionMultiplexerFactory _connectionMultiplexerFactory;
        private readonly BufferBlock<ITransport> _transportBufferBlock;
        private readonly ConfigurationOptions _redisConfiguration;
        private readonly SemaphoreSlim _semaphore;

        private IConnectionMultiplexer _connectionMultiplexer;

        public RedisTransportListener(
            Uri uri,
            IEnvelopeSerializer envelopeSerializer = null,
            ITraceWriter traceWriter = null,
            int acceptTransportBoundedCapacity = 10,
            IConnectionMultiplexerFactory connectionMultiplexerFactory = null,
            string channelNamespace = null)
            : this(ConfigurationOptions.Parse(uri?.DnsSafeHost), envelopeSerializer, traceWriter, acceptTransportBoundedCapacity, connectionMultiplexerFactory, channelNamespace)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (!uri.Scheme.Equals(RedisScheme, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid URI scheme. Expected is '{RedisScheme}'.", nameof(uri));
            }
        }

        public RedisTransportListener(
            ConfigurationOptions redisConfiguration,
            IEnvelopeSerializer envelopeSerializer = null,
            ITraceWriter traceWriter = null,
            int acceptTransportBoundedCapacity = 10,
            IConnectionMultiplexerFactory connectionMultiplexerFactory = null,
            string channelNamespace = null)
        {
            if (redisConfiguration == null) throw new ArgumentNullException(nameof(redisConfiguration));
            _redisConfiguration = redisConfiguration;
            _envelopeSerializer = envelopeSerializer ?? new JsonNetSerializer();
            _traceWriter = traceWriter;
            _channelNamespace = channelNamespace ?? RedisTransport.DefaultChannelNamespace;
            _connectionMultiplexerFactory = connectionMultiplexerFactory ?? new ConnectionMultiplexerFactory();
            _transportBufferBlock = new BufferBlock<ITransport>(
                new DataflowBlockOptions()
                {
                    BoundedCapacity = acceptTransportBoundedCapacity
                });
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public Uri[] ListenerUris => 
            _connectionMultiplexer?
            .GetEndPoints()
            .Select(e => new Uri($"{RedisScheme}://{e}")).ToArray();

        public async Task StartAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_connectionMultiplexer != null)
                {
                    throw new InvalidOperationException("The connection is already open");
                }

                _connectionMultiplexer =
                    await _connectionMultiplexerFactory.CreateAsync(_redisConfiguration).ConfigureAwait(false);

                await _connectionMultiplexer
                    .GetSubscriber()
                    .SubscribeAsync(GetListenerChannelName(_channelNamespace, RedisTransport.ServerChannelPrefix), HandleReceivedData)
                    .ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken) => 
            _transportBufferBlock.ReceiveAsync(cancellationToken);

        public async Task StopAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_connectionMultiplexer == null)
                {
                    throw new InvalidOperationException("The connection is not open");
                }

                await _connectionMultiplexer
                    .GetSubscriber()
                    .UnsubscribeAllAsync()
                    .ConfigureAwait(false);

                if (_redisConfiguration != null)
                {
                    await _connectionMultiplexer.CloseAsync();
                }

                _transportBufferBlock.Complete();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        internal static string GetListenerChannelName(string channelNamespace, string sendChannelPrefix) => $"{channelNamespace}:{sendChannelPrefix}";

        private void HandleReceivedData(RedisChannel channel, RedisValue value)
        {
            var envelopeJson = (string)value;
            _traceWriter.TraceIfEnabledAsync(envelopeJson, DataOperation.Receive).Wait();

            var envelope = _envelopeSerializer.Deserialize(envelopeJson);
            var session = envelope as Session;
            if (session == null ||
                session.State != SessionState.New)
            {
                _traceWriter.TraceAsync("RedisTransportListener: An unexpected envelope was received", DataOperation.Error).Wait();
            }
            else
            {
                var transport = new RedisTransport(
                    _connectionMultiplexer, 
                    _envelopeSerializer,
                    _traceWriter, 
                    _channelNamespace, 
                    RedisTransport.ClientChannelPrefix, 
                    RedisTransport.ServerChannelPrefix);

                _transportBufferBlock.SendAsync(transport).Wait();
                transport.ReceivedEnvelopesBufferBlock.SendAsync(envelope).Wait();
            }
        }

        public void Dispose()
        {
            _connectionMultiplexer?.Dispose();
        }
    }
}
