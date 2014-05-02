using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Utility extensions for the
    /// IChannel interface
    /// </summary>
    public static class IChannelExtensions
    {
        /// <summary>
        /// Sends the envelope using the appropriate
        /// method for its type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="envelope"></param>
        /// <returns></returns>
        public static async Task SendAsync<T>(this IChannel channel, T envelope) where T : Envelope
        {
            if (typeof(T) == typeof(Notification))
            {
                await channel.SendNotificationAsync(envelope as Notification).ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(Message))
            {
                await channel.SendMessageAsync(envelope as Message).ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(Command))
            {
                await channel.SendCommandAsync(envelope as Command).ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(Session))
            {
                await channel.SendSessionAsync(envelope as Session).ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentException("Invalid or unknown envelope type");
            }
        }
    }
}
