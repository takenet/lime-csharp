using Lime.Protocol.Http.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Http.Processors
{
    public sealed class GetNotificationsHttpProcessor : GetEnvelopesHttpProcessor<Notification>
    {
        #region Constructor

        public GetNotificationsHttpProcessor(IEnvelopeStorage<Notification> notificationStorage)
            : base(notificationStorage, Constants.NOTIFICATIONS_PATH)
        {

        }

        #endregion
    }
}