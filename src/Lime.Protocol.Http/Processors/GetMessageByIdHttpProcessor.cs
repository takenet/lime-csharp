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
    public sealed class GetMessageByIdHttpProcessor : GetEnvelopeByIdHttpProcessorBase<Message>
    {
        private readonly IDocumentSerializer _serializer;

        #region Constructor

        public GetMessageByIdHttpProcessor(IEnvelopeStorage<Message> messageStorage)
            : base(messageStorage, Constants.MESSAGES_PATH)
        {
            _serializer = new DocumentSerializer();
        }

        #endregion

        protected override HttpResponse GetEnvelopeResponse(Message envelope, HttpRequest request)
        {
            if (envelope.Content != null)
            {
                var body = _serializer.Serialize(envelope.Content);
                var contentType = envelope.Content.GetMediaType().ToString();
                return new HttpResponse(request.CorrelatorId, HttpStatusCode.OK, body: body, contentType: contentType);
            }
            else
            {
                return new HttpResponse(request.CorrelatorId, HttpStatusCode.NotFound);
            }
        }
    }
}
