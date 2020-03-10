using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;

namespace Lime.Transport.SignalR
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812: Remove internal classes that are never instantiated", Justification = "The class is instantiated via reflection by ASP.NET")]
    internal class EnvelopeHub : Hub
    {
        private readonly Channel<ITransport> _transportChannel;
        private readonly ITraceWriter _traceWriter;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly IHubContext<EnvelopeHub> _hubContext;
        private readonly HubOptions _hubOptions;
        private readonly HttpConnectionDispatcherOptions _httpConnectionDispatcherOptions;
        private readonly EnvelopeHubOptions _envelopeHubOptions;
        private readonly ConcurrentDictionary<string, Channel<string>> _clientChannels;

        public EnvelopeHub(
            Channel<ITransport> transportChannel,
            ConcurrentDictionary<string, Channel<string>> clientChannels,
            IEnvelopeSerializer envelopeSerializer,
            IHubContext<EnvelopeHub> hubContext,
            HubOptions hubOptions,
            HttpConnectionDispatcherOptions httpConnectionDispatcherOptions,
            EnvelopeHubOptions envelopeHubOptions,
            ITraceWriter traceWriter = null)
        {
            _transportChannel = transportChannel;
            _clientChannels = clientChannels;
            _traceWriter = traceWriter;
            _envelopeSerializer = envelopeSerializer;
            _hubContext = hubContext;
            _hubOptions = hubOptions;
            _httpConnectionDispatcherOptions = httpConnectionDispatcherOptions;
            _envelopeHubOptions = envelopeHubOptions;            
        }

        public async Task FromClient(string envelope)
        {
            if (_clientChannels.TryGetValue(Context.UserIdentifier, out var channel))
            {
                await channel.Writer.WriteAsync(envelope);
                return;
            }
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync().ConfigureAwait(false);

            var clientChannel = _envelopeHubOptions.BoundedCapacity < 1 ?
                Channel.CreateUnbounded<string>() :
                Channel.CreateBounded<string>(_envelopeHubOptions.BoundedCapacity);
            var transport = new ServerSignalRTransport(_hubContext, Context.UserIdentifier, clientChannel, _envelopeSerializer, _hubOptions, _httpConnectionDispatcherOptions, _traceWriter);
            _clientChannels.TryAdd(Context.UserIdentifier, clientChannel);
            
            try
            {
                await _transportChannel.Writer.WriteAsync(transport);
            }
            catch
            {
                _clientChannels.TryRemove(Context.ConnectionId, out _);
                throw;
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _clientChannels.TryRemove(Context.ConnectionId, out _);
            await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
        }
    }
}
