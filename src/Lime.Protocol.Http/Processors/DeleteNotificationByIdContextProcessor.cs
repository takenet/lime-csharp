using Lime.Protocol.Http.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lime.Protocol.Http.Processors
{
    public sealed class DeleteNotificationByIdContextProcessor : DeleteEnvelopeByIdContextProcessor<Notification>
    {
        public DeleteNotificationByIdContextProcessor(IEnvelopeStorage<Notification> notificationStorage)
            : base(notificationStorage, Constants.NOTIFICATIONS_PATH)
        {

        }
  
    }
}
