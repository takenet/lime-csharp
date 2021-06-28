using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Lime.Transport.AspNetCore.Listeners
{
    public abstract class NotificationListenerBase : INotificationListener
    {
        protected NotificationListenerBase()
            : this (_ => true)
        {
            
        }
        protected NotificationListenerBase(Predicate<Notification> filter)
        {
            Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        public Predicate<Notification> Filter { get; }

        public Task OnEnvelopeAsync(Notification envelope, CancellationToken cancellationToken) => OnNotificationAsync(envelope, cancellationToken);

        public abstract Task OnNotificationAsync(Notification notification, CancellationToken cancellationToken);
    }
}