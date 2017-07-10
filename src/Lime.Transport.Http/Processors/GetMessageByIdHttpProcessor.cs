using System.IO;
using System.Net;
using System.Text;
using Lime.Protocol;
using Lime.Protocol.Serialization;
using Lime.Transport.Http.Storage;

namespace Lime.Transport.Http.Processors
{
    public sealed class GetMessageByIdHttpProcessor : GetEnvelopeByIdHttpProcessorBase<Message>
    {
        private readonly IDocumentSerializer _serializer;

        public GetMessageByIdHttpProcessor(IEnvelopeStorage<Message> messageStorage)
            : base(messageStorage, Constants.MESSAGES_PATH)
        {
            _serializer = new DocumentSerializer();
        }

        protected override HttpResponse GetEnvelopeResponse(Message envelope, HttpRequest request)
        {
            if (envelope.Content != null)
            {
                var body = _serializer.Serialize(envelope.Content);
                var contentType = envelope.Content.GetMediaType();
                var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

                return new HttpResponse(request.CorrelatorId, HttpStatusCode.OK, contentType: contentType, bodyStream: bodyStream);
            }
            else
            {
                return new HttpResponse(request.CorrelatorId, HttpStatusCode.NotFound);
            }
        }
    }
}
