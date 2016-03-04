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
    public interface ICommandChannel : ICommandSenderChannel, ICommandReceiverChannel, ICommandProcessor
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
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendCommandAsync(Command command, CancellationToken cancellationToken);
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

    /// <summary>
    /// Defines a command processor, that allows sending a command request and awaits for a response.
    /// </summary>
    public interface ICommandProcessor
    {
        /// <summary>
        /// Processes a command request, awaiting for the response.
        /// </summary>
        /// <param name="requestCommand">The command request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Command> ProcessCommandAsync(Command requestCommand, CancellationToken cancellationToken);
    }
    
    public static class CommandSenderChannelExtensions
    {
        /// <summary>
        /// Sends a command to the remote node.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public static Task SendCommandAsync(this ICommandSenderChannel channel, Command command)
        {
            return channel.SendCommandAsync(command, CancellationToken.None);
        }
    }
}
