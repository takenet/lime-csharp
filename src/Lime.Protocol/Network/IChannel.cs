using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
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
        /// Current session state
        /// </summary>
        SessionState State { get; }
    }
}
