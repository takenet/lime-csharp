using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using StackExchange.Redis;
using static Lime.Transport.Redis.RedisTransportListener;

namespace Lime.Transport.Redis
{
    public class RedisTransport : TransportBase, IAuthenticatableTransport, IDisposable
    {
        internal static readonly string ClientChannelPrefix = "client";
        internal static readonly string ServerChannelPrefix = "server";
        internal static readonly string DefaultChannelNamespace = "global";

        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly string _channelNamespace;
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
            IEnvelopeSerializer envelopeSerializer = null,
            ITraceWriter traceWriter = null,
            IConnectionMultiplexerFactory connectionMultiplexerFactory = null,
            string channelNamespace = null)
            : this(ConfigurationOptions.Parse(uri?.DnsSafeHost), envelopeSerializer, traceWriter,connectionMultiplexerFactory, channelNamespace)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (!uri.Scheme.Equals(RedisScheme, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid URI scheme. Expected is '{RedisScheme}'.", nameof(uri));
            }
        }

        public RedisTransport(
            ConfigurationOptions redisConfiguration,
            IEnvelopeSerializer envelopeSerializer = null,
            ITraceWriter traceWriter = null,
            IConnectionMultiplexerFactory connectionMultiplexerFactory = null,
            string channelNamespace = null)
            : this(envelopeSerializer, traceWriter, channelNamespace, ServerChannelPrefix, ClientChannelPrefix)
        {
            if (redisConfiguration == null) throw new ArgumentNullException(nameof(redisConfiguration));
            _redisConfiguration = redisConfiguration;
            _connectionMultiplexerFactory = connectionMultiplexerFactory ?? new ConnectionMultiplexerFactory();
        }

        internal RedisTransport(
            IConnectionMultiplexer connectionMultiplexer,
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter,
            string channelNamespace,
            string sendChannelPrefix,
            string receiveChannelPrefix)
            : this(envelopeSerializer, traceWriter, channelNamespace, sendChannelPrefix, receiveChannelPrefix)
        {
            if (connectionMultiplexer == null) throw new ArgumentNullException(nameof(connectionMultiplexer));
            _connectionMultiplexer = connectionMultiplexer;
        }

        private RedisTransport(
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter,
            string channelNamespace,
            string sendChannelPrefix,
            string receiveChannelPrefix)
        {
            _traceWriter = traceWriter;
            _channelNamespace = channelNamespace ?? DefaultChannelNamespace;
            _sendChannelPrefix = sendChannelPrefix;
            _receiveChannelPrefix = receiveChannelPrefix;
            _envelopeSerializer = envelopeSerializer ?? new EnvelopeSerializer(new DocumentTypeResolver());
            ReceivedEnvelopesBufferBlock = new BufferBlock<Envelope>(
                new DataflowBlockOptions() {BoundedCapacity = DataflowBlockOptions.Unbounded});
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            var session = envelope as Session;
            if (session != null &&
                session.State <= SessionState.Established)
            {
                await UpdateReceiveChannelNameAsync(session, cancellationToken);
            }

            var envelopeJson = _envelopeSerializer.Serialize(envelope);

            await _traceWriter.TraceIfEnabledAsync(envelopeJson, DataOperation.Send);

            // Send to the channel or to the server prefix
            await _connectionMultiplexer
                .GetSubscriber()
                .PublishAsync(_sendChannelName ?? GetListenerChannelName(_channelNamespace, ServerChannelPrefix), envelopeJson)
                .ConfigureAwait(false);
        }
       
        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            var envelope = await ReceivedEnvelopesBufferBlock.ReceiveAsync(cancellationToken).ConfigureAwait(false);

            var session = envelope as Session;
            if (session != null &&
                session.State <= SessionState.Established)
            {
                UpdateSendChannelName(session);
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
                        .UnsubscribeAsync(_receiveChannelName)
                        .ConfigureAwait(false);
                }

                _sendChannelName = null;
                _receiveChannelName = null;

                if (_redisConfiguration != null)
                {
                    await _connectionMultiplexer.CloseAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Updates the name for sending envelopes. 
        /// The send channel name is always related to the last received session id.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <exception cref="System.InvalidOperationException">An invalid session envelope was received: The session envelope must have an id</exception>
        private void UpdateSendChannelName(Session session)
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

        /// <summary>
        /// Updates the receive channel name for receiving envelopes.
        /// The receive channel name is always related to the last sent session id.
        /// If is changed, the Redis subscription must be updated.
        /// </summary>
        private async Task UpdateReceiveChannelNameAsync(Session session, CancellationToken cancellationToken)
        {
            if (session.State == SessionState.New)
            {                
                if (!string.IsNullOrWhiteSpace(session.Id))
                {
                    throw new InvalidOperationException("The 'new' session envelope should not have an id");
                }

                if (_sendChannelName != null)
                {
                    // If the send channel name is defined, it indicates that the current transport
                    // has already received a session envelope, so we cannot and a 'new' session here.
                    throw new InvalidOperationException("Cannot send a 'new' session when other state was already received");
                }

                // Sets a temporary id for receiving the session data.
                // It will be used by the server transport to continue the session negotiation.
                session.Id = EnvelopeId.NewId();
            }

            var receiveChannelName = GetReceiveChannelName(session.Id);

            if (_receiveChannelName == null ||
                !_receiveChannelName.Equals(receiveChannelName))
            {
                var subscriber = _connectionMultiplexer.GetSubscriber();

                if (_receiveChannelName != null)
                {
                    await subscriber
                        .UnsubscribeAsync(_receiveChannelName)
                        .ConfigureAwait(false);
                }

                _receiveChannelName = receiveChannelName;

                await subscriber
                    .SubscribeAsync(_receiveChannelName, HandleReceivedData)
                    .ConfigureAwait(false);
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
        
        private string GetReceiveChannelName(string id) => $"{_channelNamespace}:{_receiveChannelPrefix}:{id}";

        private string GetSendChannelName(string id) => $"{_channelNamespace}:{_sendChannelPrefix}:{id}";

        public Task<DomainRole> AuthenticateAsync(Identity identity)
        {
            // TODO: Implement a proper authentication mechanism
            return DomainRole.RootAuthority.AsCompletedTask();
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
