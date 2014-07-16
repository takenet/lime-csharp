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
        /// The current session transport
        /// </summary>
        ITransport Transport { get; }

        /// <summary>
        /// Remote node identifier
        /// </summary>
        Node RemoteNode { get; }

        /// <summary>
        /// Remote node identifier
        /// </summary>
        Node LocalNode { get; }

        /// <summary>
        /// The session Id
        /// </summary>
        Guid SessionId { get; }

        /// <summary>
        /// Current session state
        /// </summary>
        SessionState State { get; }

    }
}
