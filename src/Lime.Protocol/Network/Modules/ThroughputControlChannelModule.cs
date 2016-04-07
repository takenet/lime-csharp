using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Util;

namespace Lime.Protocol.Network.Modules
{
    /// <summary>
    /// Defines a channel module that controls envelope throughput
    /// </summary>
    public sealed class ThroughputControlChannelModule : IChannelModule<Message>, IChannelModule<Notification>, IChannelModule<Command>, IDisposable
    {
        private readonly IChannel _channel;
        private readonly RateGate _rateGate;

        private ThroughputControlChannelModule(IChannel channel, int throughput)
        {
            _channel = channel;
            _rateGate = new RateGate(throughput, TimeSpan.FromSeconds(1));
        }

        public Task<Message> OnReceivingAsync(Message envelope, CancellationToken cancellationToken)
        {
            return envelope.AsCompletedTask();
        }

        public async Task<Message> OnSendingAsync(Message envelope, CancellationToken cancellationToken)
        {
            await _rateGate.WaitToProceedAsync(cancellationToken);
            return envelope;
        }

        public Task<Command> OnReceivingAsync(Command envelope, CancellationToken cancellationToken)
        {
            return envelope.AsCompletedTask();
        }

        public async Task<Command> OnSendingAsync(Command envelope, CancellationToken cancellationToken)
        {
            await _rateGate.WaitToProceedAsync(cancellationToken);
            return envelope;
        }

        public Task<Notification> OnReceivingAsync(Notification envelope, CancellationToken cancellationToken)
        {
            return envelope.AsCompletedTask();
        }

        public async Task<Notification> OnSendingAsync(Notification envelope, CancellationToken cancellationToken)
        {
            await _rateGate.WaitToProceedAsync(cancellationToken);
            return envelope;
        }

        public void OnStateChanged(SessionState state)
        {            
        }

        public void Dispose()
        {
            _rateGate.Dispose();
        }

        /// <summary>
        /// Creates a new instance of<see cref= "ThroughputControlChannelModule" /> class and register it for all envelope types into the specified channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="throughput">Number of allowed envelopes per second</param>
        public static ThroughputControlChannelModule CreateAndRegister(IChannel channel, int throughput = 10)
        {
            var throughputChannelModule = new ThroughputControlChannelModule(channel, throughput);
            channel.MessageModules.Add(throughputChannelModule);
            channel.NotificationModules.Add(throughputChannelModule);
            channel.CommandModules.Add(throughputChannelModule);
            return throughputChannelModule;
        }
    }
}
