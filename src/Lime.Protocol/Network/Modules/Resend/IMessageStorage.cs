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
        /// <param name="expiration"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AddAsync(string channelKey, string messageKey, Message message, DateTimeOffset expiration, CancellationToken cancellationToken);

        /// <summary>
        /// Removes a stored retry message, returning the removed value if available.
        /// </summary>
        /// <param name="channelKey"></param>
        /// <param name="messageKey"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Message> RemoveAsync(string channelKey, string messageKey, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the keys of the expired messages.
        /// </summary>
        /// <param name="channelKey"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<string>> GetExpiredMessageKeysAsync(string channelKey, CancellationToken cancellationToken);

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