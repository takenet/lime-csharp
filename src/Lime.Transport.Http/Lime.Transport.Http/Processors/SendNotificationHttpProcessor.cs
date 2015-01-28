using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;

namespace Lime.Transport.Http.Protocol.Processors
{
    public class SendNotificationHttpProcessor : SendEnvelopeHttpProcessorBase<Notification>
    {        
        #region Constructor

        public SendNotificationHttpProcessor(ITraceWriter traceWriter = null)
            : base(new HashSet<string> { Constants.HTTP_METHOD_POST }, new UriTemplate(string.Format("/{0}", Constants.NOTIFICATIONS_PATH)), null, traceWriter)
        {

        }

        #endregion

        protected override async Task FillEnvelopeAsync(Notification envelope, HttpRequest request)
        {
            if (envelope.Id == Guid.Empty)
            {
                throw new LimeException(ReasonCodes.VALIDATION_ERROR, "Invalid notification id");
            }

            string body;
            using (var streamReader = new StreamReader(request.BodyStream))
            {
                body = await streamReader.ReadToEndAsync().ConfigureAwait(false);                                             
            }

            try
            {
                envelope.Event = TypeUtil.ParseEnum<Event>(body);
            }
            catch (ArgumentException ex)
            {
                throw new LimeException(ReasonCodes.VALIDATION_ERROR, ex.Message);
            }

            if (envelope.Event == Event.Failed)
            {
                envelope.Reason = new Reason()
                {
                    Code = ReasonCodes.GENERAL_ERROR
                };

                int reasonCode;
                if (int.TryParse(request.Headers.Get(Constants.REASON_CODE_HEADER), out reasonCode))
                {
                    envelope.Reason.Code = reasonCode;
                }

                envelope.Reason.Description = request.Headers.Get(Constants.REASON_DESCRIPTION_HEADER);
            }            
        }

    }
}
