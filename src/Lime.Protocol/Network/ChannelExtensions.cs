using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Utility extensions for the IChannel interface.
    /// </summary>
    public static class ChannelExtensions
    {
        /// <summary>
        /// Sends the envelope using the appropriate method for its type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="envelope"></param>
        /// <returns></returns>
        public static Task SendAsync<T>(this IEstablishedSenderChannel channel, T envelope) where T : Envelope, new()
        {
            return SendAsync(channel, envelope, CancellationToken.None);
        }

        /// <summary>
        /// Sends the envelope using the appropriate method for its type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel">The channel.</param>
        /// <param name="envelope">The envelope.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        /// <exception cref="System.ArgumentException">Invalid or unknown envelope type</exception>
        public static async Task SendAsync<T>(this IEstablishedSenderChannel channel, T envelope, CancellationToken cancellationToken) where T : Envelope, new()
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));

            if (typeof(T) == typeof(Notification))
            {
                await channel.SendNotificationAsync(envelope as Notification, cancellationToken).ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(Message))
            {
                await channel.SendMessageAsync(envelope as Message, cancellationToken).ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(Command))
            {
                await channel.SendCommandAsync(envelope as Command, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentException("Invalid or unknown envelope type");
            }
        }
    }
}