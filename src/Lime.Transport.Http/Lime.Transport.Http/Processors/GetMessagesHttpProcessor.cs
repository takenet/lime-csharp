using Lime.Protocol;
using Lime.Transport.Http.Protocol.Storage;

namespace Lime.Transport.Http.Protocol.Processors
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
