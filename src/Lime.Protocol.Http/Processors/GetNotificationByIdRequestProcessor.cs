using Lime.Protocol.Http.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Http.Serialization;

namespace Lime.Protocol.Http.Processors
{
    public class GetNotificationByIdRequestProcessor : GetEnvelopeByIdRequestProcessor<Notification>
    {
        #region Constructor

        public GetNotificationByIdRequestProcessor(IEnvelopeStorage<Notification> notificationStorage)
            : base(notificationStorage, Constants.NOTIFICATIONS_PATH)
        {
            
        }

        #endregion

        protected override async Task WriteEnvelopeResultAsync(Notification envelope, HttpListenerResponse response)
        {
            response.ContentType = Constants.TEXT_PLAIN_HEADER_VALUE;            
            if (envelope.Reason != null)
            {
                response.Headers.Add(Constants.REASON_CODE_HEADER, envelope.Reason.Code.ToString());
                response.StatusCode = (int)envelope.Reason.ToHttpStatusCode();
                response.StatusDescription = envelope.Reason.Description;
            }

            using (var writer = new StreamWriter(response.OutputStream))
            {
                await writer.WriteAsync(envelope.Event.ToString()).ConfigureAwait(false);
            }           
        }

    }
}
