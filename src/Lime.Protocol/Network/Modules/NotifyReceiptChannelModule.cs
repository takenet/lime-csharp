using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network.Modules
{
    /// <summary>
    /// Defines a channel module that automatically send a <see cref="Notification"/> for each received message.
    /// </summary>
    public class NotifyReceiptChannelModule : ChannelModuleBase<Message>
    {
        private readonly IChannel _channel;

        public NotifyReceiptChannelModule(IChannel channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        public override async Task<Message> OnReceivingAsync(Message envelope, CancellationToken cancellationToken)
        {
            if (!envelope.Id.IsNullOrEmpty() 
                && envelope.From != null 
                && (envelope.To == null 
                    || envelope.To.Equals(_channel.LocalNode) 
                    || (envelope.To.Instance.IsNullOrEmpty() 
                        && envelope.To.ToIdentity().Equals(_channel.LocalNode.ToIdentity()))))
            {
                var notification = CreateNotification(envelope);
                await _channel.SendNotificationAsync(notification, cancellationToken);
            }

            return envelope;
        }

        protected virtual Notification CreateNotification(Message message)
        {
            return new Notification
            {
                Id = message.Id,
                To = message.GetSender(),
                Event = Event.Received
            };
        }
    }
}
