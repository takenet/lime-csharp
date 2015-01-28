using Lime.Protocol;
using Lime.Transport.Http.Protocol.Storage;

namespace Lime.Transport.Http.Protocol.Processors
{
    public sealed class DeleteNotificationByIdHttpProcessor : DeleteEnvelopeByIdHttpProcessor<Notification>
    {
        public DeleteNotificationByIdHttpProcessor(IEnvelopeStorage<Notification> notificationStorage)
            : base(notificationStorage, Constants.NOTIFICATIONS_PATH)
        {

        }
  
    }
}
