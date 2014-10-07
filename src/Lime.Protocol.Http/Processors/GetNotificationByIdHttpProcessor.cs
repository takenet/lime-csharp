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
using Lime.Protocol.Serialization;

namespace Lime.Protocol.Http.Processors
{
    public sealed class GetNotificationByIdHttpProcessor : GetEnvelopeByIdHttpProcessorBase<Notification>
    {
        #region Constructor

        public GetNotificationByIdHttpProcessor(IEnvelopeStorage<Notification> notificationStorage)
            : base(notificationStorage, Constants.NOTIFICATIONS_PATH)
        {
            
        }

        #endregion


        protected override HttpResponse GetEnvelopeResponse(Notification envelope, HttpRequest request)
        {
            HttpResponse response;
            if (envelope.Reason != null)
            {

                var body = envelope.Event.ToString().ToCamelCase();
                response = new HttpResponse(request.CorrelatorId, envelope.Reason.ToHttpStatusCode(), envelope.Reason.Description, body: body);                
                response.Headers.Add(Constants.REASON_CODE_HEADER, envelope.Reason.Code.ToString());
            }
            else
            {
                response = new HttpResponse(request.CorrelatorId, HttpStatusCode.OK);
            }

            response.Headers[HttpResponseHeader.ContentType] = Constants.TEXT_PLAIN_HEADER_VALUE;
            return response;            
        }
    }
}
