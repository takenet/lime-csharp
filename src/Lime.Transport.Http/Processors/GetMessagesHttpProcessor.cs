using Lime.Protocol;
using Lime.Transport.Http.Storage;

namespace Lime.Transport.Http.Processors
{
    public sealed class GetMessagesHttpProcessor : GetEnvelopesHttpProcessor<Message>
    {
        public GetMessagesHttpProcessor(IEnvelopeStorage<Message> messageStorage)
            : base(messageStorage, Constants.MESSAGES_PATH)
        {

        }
    }
}
