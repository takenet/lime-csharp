using System.Net;
using Lime.Protocol;
using Lime.Protocol.Serialization;
using Lime.Transport.Http.Storage;

namespace Lime.Transport.Http.Processors
{
    public sealed class GetNotificationByIdHttpProcessor : GetEnvelopeByIdHttpProcessorBase<Notification>
    {
        public GetNotificationByIdHttpProcessor(IEnvelopeStorage<Notification> notificationStorage)
            : base(notificationStorage, Constants.NOTIFICATIONS_PATH)
        {
            
        }


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
