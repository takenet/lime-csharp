using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network.Modules.Resend
{
    /// <summary>
    /// Defines a service for handling dead messages (messages without notification after retries) for the <see cref="ResendMessagesChannelModule"/> class.
    /// </summary>
    public interface IDeadMessageHandler
    {
        /// <summary>
        /// Handles a channel dead message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="channelInformation"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task HandleDeadMessageAsync(Message message, IChannelInformation channelInformation, CancellationToken cancellationToken);
    }
}
