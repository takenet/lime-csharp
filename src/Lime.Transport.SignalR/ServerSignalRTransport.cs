using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ReflectionMagic;

namespace Lime.Transport.SignalR
{
    public sealed class ServerSignalRTransport : SignalRTransport
    {
        private readonly IHubContext<EnvelopeHub> _hubContext;
        private readonly string _connectionId;
        private readonly string _userId;
        private readonly HubOptions _hubOptions;
        private readonly HttpConnectionDispatcherOptions _httpConnectionDispatcherOptions;

        internal ServerSignalRTransport(
            IHubContext<EnvelopeHub> hubContext,
            string connectionId,
            string userId,
            ChannelReader<string> envelopeChannel,
            IEnvelopeSerializer envelopeSerializer,
            HubOptions hubOptions,
            HttpConnectionDispatcherOptions httpConnectionDispatcherOptions,

            ITraceWriter traceWriter = null) : base(envelopeChannel,
                                                    envelopeSerializer,
                                                    traceWriter)
        {
            _hubContext = hubContext;
            _connectionId = connectionId;
            _userId = userId;
            _hubOptions = hubOptions;
            _httpConnectionDispatcherOptions = httpConnectionDispatcherOptions;
        }

        public override string LocalEndPoint
        {
            get
            {
                return _hubContext.GetLocalEndpoint(_userId)?.ToString();
            }
        }

        public override string RemoteEndPoint
        {
            get
            {
                return _hubContext.GetRemoteEndpoint(_userId)?.ToString();
            }
        }

        public override IReadOnlyDictionary<string, object> Options
        {
            get
            {
                var options = new RouteValueDictionary(_httpConnectionDispatcherOptions);
                new RouteValueDictionary(_hubOptions).ToList().ForEach(x => options[x.Key] = x.Value);
                

                return options;
            }
        }

        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            ThrowIfClosed();

            await base.SendAsync(envelope, cancellationToken).ConfigureAwait(false);

            string envelopeSerialized = EnvelopeSerializer.Serialize(envelope);
            await TraceWriter.TraceIfEnabledAsync(envelopeSerialized, DataOperation.Send).ConfigureAwait(false);
            var client = _hubContext.Clients.User(_userId);

            await client.SendAsync("FromServer", envelopeSerialized).ConfigureAwait(false);
        }

        protected override async Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            var client = _hubContext.Clients.User(_userId);
            await client.SendAsync("Close", cancellationToken).ConfigureAwait(false);

            await base.PerformCloseAsync(cancellationToken).ConfigureAwait(false);
        }

    }
}
