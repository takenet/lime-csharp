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
        private readonly INotificationChannel _notificationChannel;

        public NotifyReceiptChannelModule(INotificationChannel notificationChannel)
        {
            if (notificationChannel == null) throw new ArgumentNullException(nameof(notificationChannel));
            _notificationChannel = notificationChannel;
        }

        public override async Task<Message> OnReceivingAsync(Message envelope, CancellationToken cancellationToken)
        {
            if (envelope.Id != Guid.Empty &&
                envelope.From != null)
            {
                var notification = new Notification
                {
                    Id = envelope.Id,
                    To = envelope.GetSender(),
                    Event = Event.Received
                };

                await _notificationChannel.SendNotificationAsync(notification, cancellationToken);
            }

            return envelope;
        }
    }
}
