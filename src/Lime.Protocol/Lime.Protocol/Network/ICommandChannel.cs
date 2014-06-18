using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a command envelopes exchanging channel
    /// </summary>
    public interface ICommandChannel
    {
        /// <summary>
        /// Sends a command envelope to
        /// the remote node
        /// </summary>
        /// <param name="command"></param>
        Task SendCommandAsync(Command command);

        /// <summary>
        /// Receives a command
        /// from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Command> ReceiveCommandAsync(CancellationToken cancellationToken);
    }
}
