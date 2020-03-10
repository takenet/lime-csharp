using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Lime.Transport.SignalR
{
    /// <summary>
    /// Provides methods for the server to communicate with client nodes using SignalR as the underlying transport mechanism.
    /// This class cannot be inherited.
    /// </summary>
    /// <inheritdoc />
    internal sealed class ServerSignalRTransport : SignalRTransport
    {
        private const string FROM_SERVER_METHOD = "FromServer";
        private const string CLOSE_METHOD = "Close";
        private readonly IHubContext<EnvelopeHub> _hubContext;
        private readonly string _connectionId;
        private readonly HubOptions _hubOptions;
        private readonly HttpConnectionDispatcherOptions _httpConnectionDispatcherOptions;

        internal ServerSignalRTransport(
            IHubContext<EnvelopeHub> hubContext,
            string connectionId,
            Channel<string> envelopeChannel,
            IEnvelopeSerializer envelopeSerializer,
            HubOptions hubOptions,
            HttpConnectionDispatcherOptions httpConnectionDispatcherOptions,

            ITraceWriter traceWriter = null) : base(envelopeChannel,
                                                    envelopeSerializer,
                                                    traceWriter)
        {
            _hubContext = hubContext;
            _connectionId = connectionId;
            _hubOptions = hubOptions;
            _httpConnectionDispatcherOptions = httpConnectionDispatcherOptions;
        }

        /// <inheritdoc />
        public override string LocalEndPoint
        {
            get
            {
                try
                {
                    return _hubContext.GetLocalEndpoint(_connectionId)?.ToString();
                }
                catch (MissingMemberException ex)
                {
                    TraceWriter.TraceIfEnabledAsync(ex.ToString(), DataOperation.Error);
                    return string.Empty;
                }
            }
        }

        /// <inheritdoc />
        public override string RemoteEndPoint
        {
            get
            {
                try
                {
                    return _hubContext.GetRemoteEndpoint(_connectionId)?.ToString();
                }
                catch (MissingMemberException ex)
                {
                    TraceWriter.TraceIfEnabledAsync(ex.ToString(), DataOperation.Error);
                    return string.Empty;
                }
            }
        }

        /// <inheritdoc />
        public override IReadOnlyDictionary<string, object> Options
        {
            get
            {
                var options = new RouteValueDictionary(_httpConnectionDispatcherOptions);
                new RouteValueDictionary(_hubOptions).ToList().ForEach(x => options[x.Key] = x.Value);
                
                return options;
            }
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when the transport is not connected.</exception>
        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();

            await base.SendAsync(envelope, cancellationToken).ConfigureAwait(false);

            string envelopeSerialized = EnvelopeSerializer.Serialize(envelope);
            await TraceWriter.TraceIfEnabledAsync(envelopeSerialized, DataOperation.Send).ConfigureAwait(false);
            var client = _hubContext.Clients.Client(_connectionId);

            await client.SendAsync(FROM_SERVER_METHOD, envelopeSerialized).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            var client = _hubContext.Clients.Client(_connectionId);

            // This is the current suggested workaround, as there is currently no supported way to forcibly close 
            // a connection from server side outside of the Hub context.
            // https://github.com/dotnet/aspnetcore/issues/5333 tracks the feature request for this 
            // and also describes an alternative, unreliable workaround.
            await client.SendAsync(CLOSE_METHOD, cancellationToken).ConfigureAwait(false);

            await base.PerformCloseAsync(cancellationToken).ConfigureAwait(false);
        }

    }
}
