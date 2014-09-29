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
    public class SendMessageProcessor : SendEnvelopeProcessorBase<Message>, IRequestProcessor
    {
        #region Constructor

        public SendMessageProcessor(string messagesPath, ConcurrentDictionary<Guid, HttpListenerResponse> pendingResponsesDictionary)
            : base(new[] { Constants.HTTP_METHOD_POST }, new UriTemplate(string.Format("/{0}", Constants.MESSAGES_PATH)), new DocumentSerializer(), pendingResponsesDictionary)
        {

        }

        #endregion


        public override Task ProcessAsync(HttpListenerContext context, ServerHttpTransport transport, CancellationToken cancellationToken)
        {            
            if (context.Request.HttpMethod.Equals(Constants.HTTP_METHOD_POST))
            {
                return base.ProcessAsync(context, transport, cancellationToken);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                context.Response.Close();
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
