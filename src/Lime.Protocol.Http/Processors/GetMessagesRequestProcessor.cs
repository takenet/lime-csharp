using Lime.Protocol.Http.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Http.Processors
{
    public class GetMessagesRequestProcessor : GetEnvelopesRequestProcessor<Message>
    {
        #region Constructor

        public GetMessagesRequestProcessor(IEnvelopeStorage<Message> messageStorage)
            : base(messageStorage, Constants.MESSAGES_PATH)
        {

        }

        #endregion
    }
}
