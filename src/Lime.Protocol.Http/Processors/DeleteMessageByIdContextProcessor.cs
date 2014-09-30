using Lime.Protocol.Http.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lime.Protocol.Http.Processors
{
    public sealed class DeleteMessageByIdContextProcessor : DeleteEnvelopeByIdContextProcessor<Message>
    {
        public DeleteMessageByIdContextProcessor(IEnvelopeStorage<Message> messageStorage)
            : base(messageStorage, Constants.MESSAGES_PATH)
        {

        }
  
    }
}
