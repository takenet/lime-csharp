using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace Lime.Transport.SignalR
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "ASP.NET Core doesn't have a SynchronizationContext")]
    internal class EnvelopeHub : Hub
    {
        private readonly Channel<ITransport> _transportChannel;
        private readonly ITraceWriter _traceWriter;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly IHubContext<EnvelopeHub> _hubContext;

        public EnvelopeHub(Channel<ITransport> transportChannel, ITraceWriter traceWriter, IEnvelopeSerializer envelopeSerializer, IHubContext<EnvelopeHub> hubContext)
        {
            _transportChannel = transportChannel;
            _traceWriter = traceWriter;
            _envelopeSerializer = envelopeSerializer;
            _hubContext = hubContext;
        }

        private static ConcurrentDictionary<string, (SignalRTransport Transport, Channel<string> Channel)> ClientTransports { get; } = new ConcurrentDictionary<string, (SignalRTransport, Channel<string>)>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "transport is being written into the channel and stored somewhere else")]
        public async Task FromClient(string envelope)
        {
            if (ClientTransports.TryGetValue(Context.ConnectionId, out var client))
            {
                await client.Channel.Writer.WriteAsync(envelope);
                return;
            }

            var channel = Channel.CreateUnbounded<string>(); // TODO bounded
            var transport = new SignalRTransport(_hubContext, Context.ConnectionId, channel, _envelopeSerializer, _traceWriter);
            ClientTransports.TryAdd(Context.ConnectionId, (transport, channel));

            try
            {

                await _transportChannel.Writer.WriteAsync(transport);
            }
            catch
            {
                ClientTransports.TryRemove(Context.ConnectionId, out _);
                transport.Dispose();
                throw;
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (ClientTransports.TryRemove(Context.ConnectionId, out var client))
            {
                client.Transport.Dispose();
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
