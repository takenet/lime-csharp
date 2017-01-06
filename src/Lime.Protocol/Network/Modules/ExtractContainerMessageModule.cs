using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network.Modules
{
    /// <summary>
    /// Extract the value of container documents directly to the message.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Network.Modules.ChannelModuleBase{Message}" />
    public class ExtractContainerMessageModule : ChannelModuleBase<Message>
    {
        public override Task<Message> OnReceivingAsync(Message envelope, CancellationToken cancellationToken)
        {
            if (envelope.Content is IDocumentContainer)
            {
                envelope.Content = ((IDocumentContainer)envelope.Content).GetDocument();
            }

            return base.OnReceivingAsync(envelope, cancellationToken);
        }
    }
}
