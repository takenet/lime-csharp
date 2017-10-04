using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network.Modules.Resend
{
    /// <summary>
    /// Defines a service for storing messages for the <see cref="ResendMessagesChannelModule2"/> module.
    /// </summary>
    public interface IMessageStorage
    {
        /// <summary>
        /// Stores a message for further retrying.
        /// </summary>
        /// <param name="channelKey"></param>
        /// <param name="messageKey"></param>
        /// <param name="message"></param>
        /// <param name="resendAt"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AddAsync(string channelKey, string messageKey, Message message, DateTimeOffset resendAt, CancellationToken cancellationToken);

        /// <summary>
        /// Removes a stored retry message, returning the removed value if available.
        /// </summary>
        /// <param name="channelKey"></param>
        /// <param name="messageKey"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Message> RemoveAsync(string channelKey, string messageKey, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the keys of the messages that should be resent given the specified reference date.
        /// </summary>
        /// <param name="channelKey"></param>
        /// <param name="reference"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<string>> GetMessagesToResendKeysAsync(string channelKey, DateTimeOffset reference, CancellationToken cancellationToken);

        /// <summary>
        /// Optionally stores a dead message.
        /// </summary>
        /// <param name="channelKey"></param>
        /// <param name="messageKey"></param>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AddDeadMessageAsync(string channelKey, string messageKey, Message message, CancellationToken cancellationToken);
    }
}