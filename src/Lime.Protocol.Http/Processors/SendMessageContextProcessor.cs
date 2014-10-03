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

        public override async Task ProcessAsync(HttpListenerContext context, ServerHttpTransport transport, UriTemplateMatch match, CancellationToken cancellationToken)
        {
            var envelope = await GetEnvelopeFromRequestAsync(context.Request).ConfigureAwait(false);

            bool isAsync = envelope.Id == Guid.Empty;
            if (!isAsync)
            {
                bool.TryParse(context.Request.QueryString.Get(Constants.ASYNC_QUERY), out isAsync);
            }

            await ProcessEnvelopeAsync(envelope, transport, context.Response, isAsync, cancellationToken).ConfigureAwait(false); 
        }

        protected override async Task FillEnvelopeAsync(Message envelope, HttpListenerRequest request)
        {
            envelope.Content = await ParseDocumentAsync(request).ConfigureAwait(false);
            if (envelope.Content == null)
            {
                throw new LimeException(ReasonCodes.VALIDATION_EMPTY_DOCUMENT, "Invalid or empty content");
            }
        }
    }
}
