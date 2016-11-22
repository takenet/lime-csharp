namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines the workflow for synchronous command processing.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Network.ICommandProcessor" />
    public interface IChannelCommandProcessor : ICommandProcessor
    {
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