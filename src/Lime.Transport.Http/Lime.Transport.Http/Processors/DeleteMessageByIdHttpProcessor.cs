using Lime.Protocol;
using Lime.Transport.Http.Storage;

namespace Lime.Transport.Http.Processors
{
    public sealed class DeleteMessageByIdHttpProcessor : DeleteEnvelopeByIdHttpProcessor<Message>
    {
        public DeleteMessageByIdHttpProcessor(IEnvelopeStorage<Message> messageStorage)
            : base(messageStorage, Constants.MESSAGES_PATH)
        {

        }
  
    }
}
