using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a communication channel 
    /// for the protocol
    /// </summary>
    public interface IChannel : IMessageChannel, ICommandChannel, INotificationChannel, ISessionChannel
    {
        /// <summary>
        /// Gets the current session transport.
        /// </summary>
        ITransport Transport { get; }

        /// <summary>
        /// Gets the remote node identifier.
        /// </summary>
        Node RemoteNode { get; }

        /// <summary>
        /// Gets the local node identifier.
        /// </summary>
        Node LocalNode { get; }

        /// <summary>
        /// Gets the current session Id.
        /// </summary>
        Guid SessionId { get; }

        /// <summary>
        /// Gets the current session state.
        /// </summary>
        SessionState State { get; }

        /// <summary>
        /// Gets the message modules for processing sent and received messages.
        /// </summary>
        ICollection<IChannelModule<Message>> MessageModules { get; }

        /// <summary>
        /// Gets the notification modules for processing sent and received notifications.
        /// </summary>
        ICollection<IChannelModule<Notification>> NotificationModules { get; }

        /// <summary>
        /// Gets the command modules for processing sent and received commands.
        /// </summary>
        ICollection<IChannelModule<Command>> CommandModules { get; }
    }
}
