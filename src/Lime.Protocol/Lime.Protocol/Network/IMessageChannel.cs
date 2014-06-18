using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        /// Receives a message
        /// from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken);
    }
}
