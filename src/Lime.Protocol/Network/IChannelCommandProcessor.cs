using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines the workflow for synchronous command processing.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Network.ICommandProcessor" />
    public interface IChannelCommandProcessor 
    {
        /// <summary>
        /// Processes the command asynchronous.
        /// </summary>
        /// <param name="commandSenderChannel">The command sender channel.</param>
        /// <param name="requestCommand">The request command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Command> ProcessCommandAsync(ICommandSenderChannel commandSenderChannel, Command requestCommand, CancellationToken cancellationToken);

        /// <summary>
        /// Try to submit a command as a result for a pending command request.
        /// </summary>
        /// <param name="responseCommand">The response command.</param>
        /// <returns></returns>
        bool TrySubmitCommandResult(Command responseCommand);

        /// <summary>
        /// Cancels all pending command requests.
        /// </summary>
        void CancelAll();
    }
}