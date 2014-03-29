using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a notification envelopes exchanging channel
    /// </summary>
    public interface INotificationChannel
    {
        /// <summary>
        /// Sends a notification to the
        /// remote node
        /// </summary>
        /// <param name="notification"></param>
        Task SendNotificationAsync(Notification notification);

        /// <summary>
        /// Occurs when a notification is 
        /// received by the node
        /// </summary>
        event EventHandler<EnvelopeEventArgs<Notification>> NotificationReceived;
    }
}
