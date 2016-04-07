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
        private readonly bool _notifyReceiptWhenMessageReceived;
        private readonly bool _notifyReceiptWhenMessageConsumed;
        private readonly bool _notifyReceiptWhenMessageFailed;

        public NotifyReceiptChannelModule(INotificationChannel notificationChannel, bool notifyReceiptWhenMessageReceived = false, bool notifyReceiptWhenMessageConsumed = false, bool notifyReceiptWhenMessageFailed = false)
        {
            if (notificationChannel == null) throw new ArgumentNullException(nameof(notificationChannel));
            _notificationChannel = notificationChannel;
            _notifyReceiptWhenMessageReceived = notifyReceiptWhenMessageReceived;
            _notifyReceiptWhenMessageConsumed = notifyReceiptWhenMessageConsumed;
            _notifyReceiptWhenMessageFailed = notifyReceiptWhenMessageFailed;
        }

        public override async Task<Message> OnReceivingAsync(Message envelope, CancellationToken cancellationToken)
        {
            if (_notifyReceiptWhenMessageReceived)
                return await SendNotificationAsync(envelope, cancellationToken, Event.Received);
            return envelope;
        }

        public override async Task<Message> OnConsumingAsync(Message envelope, CancellationToken cancellationToken)
        {
            if (_notifyReceiptWhenMessageConsumed)
                return await SendNotificationAsync(envelope, cancellationToken, Event.Consumed);
            return envelope;
        }

        public override async Task<Message> OnFailingAsync(Message envelope, CancellationToken cancellationToken)
        {
            if (_notifyReceiptWhenMessageFailed)
                return await SendNotificationAsync(envelope, cancellationToken, Event.Failed);
            return envelope;
        }

        private async Task<Message> SendNotificationAsync(Message envelope, CancellationToken cancellationToken, Event @event)
        {
            if (envelope.Id != Guid.Empty &&
                envelope.From != null)
            {
                var notification = new Notification
                {
                    Id = envelope.Id,
                    To = envelope.GetSender(),
                    Event = @event
                };

                await _notificationChannel.SendNotificationAsync(notification, cancellationToken);
            }
            return envelope;
        }

    }
}
