using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Transport.AspNetCore;
using Lime.Transport.AspNetCore.Listeners;
using Microsoft.Extensions.Logging;

namespace Lime.Sample.AspNetCore.Listeners
{
    public class MessageListener : MessageListenerBase
    {
        private readonly ILogger<MessageListener> _logger;
        private readonly ChannelContext _channelContext;

        public MessageListener(ILogger<MessageListener> logger, ChannelContext channelContext)
        {
            _logger = logger;
            _channelContext = channelContext;
        }

        public override async Task OnMessageAsync(Message message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Message received");

            if (message.Id != null)
            {
                await _channelContext.Channel.SendNotificationAsync(
                    new Notification(message.Id)
                    {
                        Event = Event.Accepted
                    }, 
                    cancellationToken);
            }

            ISenderChannel? destinationChannel = null;
            
            if (message.To != null)
            {
                destinationChannel = _channelContext.GetChannel(message.To);
            }

            if (destinationChannel != null)
            {
                await destinationChannel.SendMessageAsync(message, cancellationToken);
            }
            else
            {
                await _channelContext.Channel.SendNotificationAsync(
                    new Notification(message.Id)
                    {
                        Event = Event.Failed,
                        Reason = new Reason()
                        {
                            Code = ReasonCodes.ROUTING_DESTINATION_NOT_FOUND,
                            Description = "Destination not found"
                        }
                        
                    }, 
                    cancellationToken);
            }
        }
    }
}