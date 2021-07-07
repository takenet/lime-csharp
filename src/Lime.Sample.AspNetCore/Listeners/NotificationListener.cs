using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Transport.AspNetCore;
using Lime.Transport.AspNetCore.Listeners;
using Microsoft.Extensions.Logging;

namespace Lime.Sample.AspNetCore.Listeners
{
    public class NotificationListener : NotificationListenerBase
    {
        private readonly ILogger<NotificationListener> _logger;

        public NotificationListener(ILogger<NotificationListener> logger)
        {
            _logger = logger;
        }
        
        public override Task OnNotificationAsync(Notification notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Notification {Id} received with event {Event}", notification.Id, notification.Event);
            
            return Task.CompletedTask;
        }
    }
}