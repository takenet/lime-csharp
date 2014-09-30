using Lime.Protocol.Http.Serialization;
using Lime.Protocol.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Http.Processors
{
    public sealed class SendMessageContextProcessor : SendEnvelopeRequestContextBase<Message>, IContextProcessor
    {
        #region Constructor

        public SendMessageContextProcessor(ConcurrentDictionary<Guid, HttpListenerResponse> pendingResponsesDictionary, ITraceWriter traceWriter = null)
            : base(new HashSet<string> { Constants.HTTP_METHOD_POST }, new UriTemplate(string.Format("/{0}", Constants.MESSAGES_PATH)), new DocumentSerializer(), pendingResponsesDictionary, traceWriter)
        {

        }

        #endregion


        public override Task ProcessAsync(HttpListenerContext context, ServerHttpTransport transport, UriTemplateMatch match, CancellationToken cancellationToken)
        {            
            if (context.Request.HttpMethod.Equals(Constants.HTTP_METHOD_POST))
            {
                return base.ProcessAsync(context, transport, match, cancellationToken);
            }
            else
            {
                context.Response.SendResponse(HttpStatusCode.MethodNotAllowed);
                return Task.FromResult<object>(null);
            }            
        }


        protected override async Task<Message> ParseEnvelopeAsync(HttpListenerRequest request)
        {
            Message message = null;

            var content = await ParseDocumentAsync(request).ConfigureAwait(false);
            if (content != null)
            {
                message = new Message()
                {
                    Content = content
                };
                FillEnvelopeFromRequest(message, request);
            }
            else
            {
                throw new LimeException(ReasonCodes.VALIDATION_EMPTY_DOCUMENT, "Invalid or empty content");
            }

            return message;
        }
    }
}
