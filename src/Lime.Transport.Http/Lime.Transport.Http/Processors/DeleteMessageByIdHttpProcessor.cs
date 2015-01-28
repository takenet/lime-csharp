using Lime.Protocol;
using Lime.Transport.Http.Protocol.Storage;

namespace Lime.Transport.Http.Protocol.Processors
{
    public sealed class DeleteMessageByIdHttpProcessor : DeleteEnvelopeByIdHttpProcessor<Message>
    {
        public DeleteMessageByIdHttpProcessor(IEnvelopeStorage<Message> messageStorage)
            : base(messageStorage, Constants.MESSAGES_PATH)
        {

        }
  
    }
}
