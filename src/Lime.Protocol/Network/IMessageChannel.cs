using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a message envelopes exchanging channel
    /// </summary>
    public interface IMessageChannel
    {
        /// <summary>
        /// Sends a message to the
        /// remote node
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendMessageAsync(Message message);

        /// <summary>
        /// Occurs when a message is 
        /// received by the node
        /// </summary>
        event EventHandler<EnvelopeEventArgs<Message>> MessageReceived;
    }
}
