using Lime.Protocol.Http.Serialization;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Http.Processors
{
    public sealed class SendMessageHttpProcessor : SendEnvelopeHttpProcessorBase<Message>
    {
        #region Constructor

        public SendMessageHttpProcessor(ITraceWriter traceWriter = null)
            : base(new HashSet<string> { Constants.HTTP_METHOD_POST }, new UriTemplate(string.Format("/{0}", Constants.MESSAGES_PATH)), new DocumentSerializer(), traceWriter)
        {

        }

        #endregion

        public override async Task<HttpResponse> ProcessAsync(HttpRequest request, UriTemplateMatch match, ITransportSession transport, CancellationToken cancellationToken)
        {
            Event waitUntilEvent;

            if (Enum.TryParse<Event>(request.QueryString.Get(Constants.WAIT_UNTIL_QUERY), true, out waitUntilEvent))
            {
                var message = await GetEnvelopeFromRequestAsync(request).ConfigureAwait(false);
                try
                {
                    var notification = await transport.ProcessMessageAsync(message, waitUntilEvent, cancellationToken).ConfigureAwait(false);

                    if (notification.Event != Event.Failed)
                    {
                        return new HttpResponse(request.CorrelatorId, HttpStatusCode.Created, body: notification.Id.ToString());
                    }
                    else if (notification.Reason != null)
                    {
                        return new HttpResponse(request.CorrelatorId, notification.Reason.ToHttpStatusCode(), notification.Reason.Description);
                    }
                    else
                    {
                        return new HttpResponse(request.CorrelatorId, HttpStatusCode.ServiceUnavailable);
                    }
                }
                catch (ArgumentException)
                {
                    return new HttpResponse(request.CorrelatorId, HttpStatusCode.Conflict);
                }
            }
            else
            {
                return await base.ProcessAsync(request, match, transport, cancellationToken).ConfigureAwait(false);
            }
        }

        protected override async Task FillEnvelopeAsync(Message envelope, HttpRequest request)
        {
            envelope.Content = await ParseDocumentAsync(request).ConfigureAwait(false);
            if (envelope.Content == null)
            {
                throw new LimeException(ReasonCodes.VALIDATION_EMPTY_DOCUMENT, "Invalid or empty content");
            }
        }
    }
}
