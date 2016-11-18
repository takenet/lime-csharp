using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using StackExchange.Redis;

namespace Lime.Transport.Redis
{
    public class RedisTransport : TransportBase, IAuthenticatableTransport, IDisposable
    {
        internal static readonly string ClientChannelPrefix = "client";
        internal static readonly string ServerChannelPrefix = "server";

        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly string _sendChannelPrefix;
        private readonly string _receiveChannelPrefix;
        private readonly ConfigurationOptions _redisConfiguration;
        private readonly IConnectionMultiplexerFactory _connectionMultiplexerFactory;
        private readonly SemaphoreSlim _semaphore;        
        internal readonly BufferBlock<Envelope> ReceivedEnvelopesBufferBlock;

        private IConnectionMultiplexer _connectionMultiplexer;
        private string _sendChannelName;
        private string _receiveChannelName;

        public RedisTransport(
            Uri uri,
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null,
            IConnectionMultiplexerFactory connectionMultiplexerFactory = null)
            : this(
                ConfigurationOptions.Parse(uri?.DnsSafeHost), envelopeSerializer, traceWriter,
                connectionMultiplexerFactory)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (!uri.Scheme.Equals(RedisTransportListener.RedisScheme, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid URI scheme. Expected is '{RedisTransportListener.RedisScheme}'.",
                    nameof(uri));
            }
        }

        public RedisTransport(
            ConfigurationOptions redisConfiguration,
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null,
            IConnectionMultiplexerFactory connectionMultiplexerFactory = null)
            : this(envelopeSerializer, traceWriter, ServerChannelPrefix, ClientChannelPrefix)
        {
            if (redisConfiguration == null) throw new ArgumentNullException(nameof(redisConfiguration));
            _redisConfiguration = redisConfiguration;
            _connectionMultiplexerFactory = connectionMultiplexerFactory ?? new ConnectionMultiplexerFactory();
        }

        internal RedisTransport(
            IConnectionMultiplexer connectionMultiplexer,
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter,
            string sendChannelPrefix,
            string receiveChannelPrefix)
            : this(envelopeSerializer, traceWriter, sendChannelPrefix, receiveChannelPrefix)
        {
            if (connectionMultiplexer == null) throw new ArgumentNullException(nameof(connectionMultiplexer));
            _connectionMultiplexer = connectionMultiplexer;
        }

        private RedisTransport(
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter,
            string sendChannelPrefix,
            string receiveChannelPrefix)
        {
            _traceWriter = traceWriter;
            _sendChannelPrefix = sendChannelPrefix;
            _receiveChannelPrefix = receiveChannelPrefix;
            _envelopeSerializer = envelopeSerializer;
            ReceivedEnvelopesBufferBlock = new BufferBlock<Envelope>(
                new DataflowBlockOptions() {BoundedCapacity = DataflowBlockOptions.Unbounded});
            _semaphore = new SemaphoreSlim(1, 1);
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

                    // Sets a temporary id for receiving the session data.
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
                .PublishAsync(_sendChannelName ?? ServerChannelPrefix, envelopeJson)
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
                    throw new InvalidOperationException(
                        "An invalid session envelope was received: The session envelope must have an id");
                }

                _sendChannelName = GetSendChannelName(session.Id);
                // Removes the temporary id
                if (session.State == SessionState.New) session.Id = null;
            }

            return envelope;
        }

        public override bool IsConnected => _connectionMultiplexer != null;

        protected override async Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_connectionMultiplexer == null)
                {
                    _connectionMultiplexer =
                        await _connectionMultiplexerFactory.CreateAsync(_redisConfiguration).ConfigureAwait(false);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        protected override async Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_connectionMultiplexer == null) throw new InvalidOperationException("The transport is not open");
                if (_receiveChannelName != null)
                {
                    await _connectionMultiplexer
                        .GetSubscriber()
                        .UnsubscribeAsync(_receiveChannelName).WithCancellation(cancellationToken);
                }

                _sendChannelName = null;
                _receiveChannelName = null;

                if (_redisConfiguration != null)
                {
                    await _connectionMultiplexer.CloseAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
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
        public Task<DomainRole> AuthenticateAsync(Identity identity)
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_redisConfiguration != null)
                {
                    _connectionMultiplexer.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
