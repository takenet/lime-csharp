using Lime.Protocol.Http.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lime.Protocol.Http.Processors
{
    public sealed class DeleteMessageByIdHttpProcessor : DeleteEnvelopeByIdHttpProcessor<Message>
    {
        public DeleteMessageByIdHttpProcessor(IEnvelopeStorage<Message> messageStorage)
            : base(messageStorage, Constants.MESSAGES_PATH)
        {

        }
  
    }
}
