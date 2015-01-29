using Lime.Protocol;
using Lime.Transport.Http.Storage;

namespace Lime.Transport.Http.Processors
{
    public sealed class GetMessagesHttpProcessor : GetEnvelopesHttpProcessor<Message>
    {
        #region Constructor

        public GetMessagesHttpProcessor(IEnvelopeStorage<Message> messageStorage)
            : base(messageStorage, Constants.MESSAGES_PATH)
        {

        }

        #endregion
    }
}
