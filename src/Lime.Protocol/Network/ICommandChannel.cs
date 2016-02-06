using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a channel to exchange command envelopes.
    /// </summary>
    public interface ICommandChannel : ICommandSenderChannel, ICommandReceiverChannel
    {

    }

    /// <summary>
    /// Defines a channel to send command envelopes.
    /// </summary>
    public interface ICommandSenderChannel
    {
        /// <summary>
        /// Sends a command to the remote node.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        Task SendCommandAsync(Command command);
    }

    /// <summary>
    /// Defines a channel to receive command envelopes.
    /// </summary>
    public interface ICommandReceiverChannel
    {
        /// <summary>
        /// Receives a command from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Command> ReceiveCommandAsync(CancellationToken cancellationToken);
    }
}
