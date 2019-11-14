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

            switch (envelope)
            {
                case Notification notification:
                    await channel.SendNotificationAsync(notification, cancellationToken).ConfigureAwait(false);
                    break;
                
                case Message message:
                    await channel.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
                    break;
                
                case Command command:
                    await channel.SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
                    break;
                
                default:
                    throw new ArgumentException("Invalid or unknown envelope type");    
            }
        }

        /// <summary>
        /// Sends a <see cref="Lime.Protocol.Message"/> and flushes the channel send buffer.
        /// </summary>
        public static async Task SendMessageAndFlushAsync(this IChannel channel, Message message, CancellationToken cancellationToken)
        {
            await channel.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
            await channel.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Sends a <see cref="Lime.Protocol.Notification"/> and flushes the channel send buffer.
        /// </summary>
        public static async Task SendNotificationAndFlushAsync(this IChannel channel, Notification notification, CancellationToken cancellationToken)
        {
            await channel.SendNotificationAsync(notification, cancellationToken).ConfigureAwait(false);
            await channel.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Sends a <see cref="Lime.Protocol.Command"/> and flushes the channel send buffer.
        /// </summary>
        public static async Task SendCommandAndFlushAsync(this IChannel channel, Command command, CancellationToken cancellationToken)
        {
            await channel.SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
            await channel.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}