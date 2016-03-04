using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a channel to exchange notification envelopes.
    /// </summary>
    public interface INotificationChannel : INotificationSenderChannel, INotificationReceiverChannel
    {

    }

    /// <summary>
    /// Defines a channel to send notification envelopes.
    /// </summary>
    public interface INotificationSenderChannel
    {
        /// <summary>
        /// Sends a notification to the remote node.
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Defines a channel to receive notification envelopes.
    /// </summary>
    public interface INotificationReceiverChannel
    {
        /// <summary>
        /// Receives a notification from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Notification> ReceiveNotificationAsync(CancellationToken cancellationToken);
    }

    public static class NotificationSenderChannelExtensions
    {
        /// <summary>
        /// Sends a notification to the remote node.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="notification"></param>
        /// <returns></returns>
        public static Task SendNotificationAsync(this INotificationSenderChannel channel, Notification notification)
        {
            return channel.SendNotificationAsync(notification, CancellationToken.None);
        }
    }
}
