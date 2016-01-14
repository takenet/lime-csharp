using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network.Modules
{
    public sealed class NotifyReceiptChannelModule : ChannelModuleBase<Message>
    {
        private readonly INotificationChannel _notificationChannel;

        public NotifyReceiptChannelModule(INotificationChannel notificationChannel)
        {
            if (notificationChannel == null) throw new ArgumentNullException(nameof(notificationChannel));
            _notificationChannel = notificationChannel;
        }

        public override async Task<Message> OnReceiving(Message envelope, CancellationToken cancellationToken)
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

                await _notificationChannel.SendNotificationAsync(notification);
            }

            return envelope;
        }
    }
}
