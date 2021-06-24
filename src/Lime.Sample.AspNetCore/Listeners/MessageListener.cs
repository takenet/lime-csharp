using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Transport.AspNetCore;
using Microsoft.Extensions.Logging;

namespace Lime.Sample.AspNetCore.Listeners
{
    public class MessageListener : MessageListenerBase
    {
        private readonly ILogger<MessageListener> _logger;

        public MessageListener(ILogger<MessageListener> logger)
        {
            _logger = logger;
        }

        public override async Task OnMessageAsync(Message message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Message received");

            if (message.Id != null)
            {
                await Channel.SendNotificationAsync(
                    new Notification(message.Id)
                    {
                        Event = Event.Accepted
                    }, 
                    cancellationToken);
            }

            ISenderChannel destinationChannel = null;
            
            if (message.To != null)
            {
                destinationChannel = GetChannel(message.To);
            }

            if (destinationChannel != null)
            {
                await destinationChannel.SendMessageAsync(message, cancellationToken);
            }
            else
            {
                await Channel.SendNotificationAsync(
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