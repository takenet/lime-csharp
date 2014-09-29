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
    public class GetMessageByIdRequestProcessor : GetEnvelopeByIdRequestProcessor<Message>
    {
        private readonly IDocumentSerializer _serializer;

        #region Constructor

        public GetMessageByIdRequestProcessor(IEnvelopeStorage<Message> messageStorage)
            : base(messageStorage, Constants.MESSAGES_PATH)
        {
            _serializer = new DocumentSerializer();
        }

        #endregion

        protected override async Task WriteEnvelopeResultAsync(Message envelope, HttpListenerResponse response)
        {            
            if (envelope.Content != null)
            {
                response.ContentType = envelope.Content.GetMediaType().ToString();
                using (var writer = new StreamWriter(response.OutputStream))
                {
                    await writer.WriteAsync(_serializer.Serialize(envelope.Content)).ConfigureAwait(false);
                }
            }
        }

    }
}
