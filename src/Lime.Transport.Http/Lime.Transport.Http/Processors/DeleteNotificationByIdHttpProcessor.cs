using Lime.Protocol;
using Lime.Transport.Http.Storage;

namespace Lime.Transport.Http.Processors
{
    public sealed class DeleteNotificationByIdHttpProcessor : DeleteEnvelopeByIdHttpProcessor<Notification>
    {
        public DeleteNotificationByIdHttpProcessor(IEnvelopeStorage<Notification> notificationStorage)
            : base(notificationStorage, Constants.NOTIFICATIONS_PATH)
        {

        }
  
    }
}
