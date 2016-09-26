using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network.Modules
{
    /// <summary>
    /// Defines a channel module that automatically send a <see cref="Event.Received"/> <see cref="Notification"/> for each received message.
    /// </summary>
    /// <seealso cref="ChannelModuleBase{T}.Protocol.Message}" />
    public sealed class NotifyReceiptChannelModule : ChannelModuleBase<Message>
    {
        private readonly IChannel _channel;

        public NotifyReceiptChannelModule(IChannel channel)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            _channel = channel;
        }

        public override async Task<Message> OnReceivingAsync(Message envelope, CancellationToken cancellationToken)
        {
            var destination = envelope.To;

            if (!envelope.Id.IsNullOrEmpty() &&
                envelope.From != null &&
                (destination == null || destination.Equals(_channel.LocalNode) || (destination.Instance.IsNullOrEmpty() && destination.ToIdentity().Equals(_channel.LocalNode.ToIdentity()))))
            {
                var notification = new Notification
                {
                    Id = envelope.Id,
                    To = envelope.GetSender(),
                    Event = Event.Received
                };

                await _channel.SendNotificationAsync(notification, cancellationToken);
            }

            return envelope;
        }
    }
}
