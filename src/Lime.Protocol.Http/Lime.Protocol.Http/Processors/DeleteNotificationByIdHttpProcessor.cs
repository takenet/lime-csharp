using Lime.Protocol.Http.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lime.Protocol.Http.Processors
{
    public sealed class DeleteNotificationByIdHttpProcessor : DeleteEnvelopeByIdHttpProcessor<Notification>
    {
        public DeleteNotificationByIdHttpProcessor(IEnvelopeStorage<Notification> notificationStorage)
            : base(notificationStorage, Constants.NOTIFICATIONS_PATH)
        {

        }
  
    }
}
