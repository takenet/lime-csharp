using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a channel to exchange message envelopes.
    /// </summary>
    public interface IMessageChannel : IMessageSenderChannel, IMessageReceiverChannel
    {

    }

    /// <summary>
    /// Defines a channel to send message envelopes.
    /// </summary>
    public interface IMessageSenderChannel
    {
        /// <summary>
        /// Sends a message to the remote node.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendMessageAsync(Message message, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Defines a channel to receive message envelopes.
    /// </summary>
    public interface IMessageReceiverChannel
    {
        /// <summary>
        /// Receives a message from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken);
    }

    public static class MessageSenderChannelExtensions
    {
        /// <summary>
        /// Sends a message to the remote node.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Task SendMessageAsync(this IMessageSenderChannel channel, Message message)
        {
            return channel.SendMessageAsync(message, CancellationToken.None);
        }
    }
}
