using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a session envelopes exchanging channel
    /// </summary>
    public interface ISessionChannel
    {
        /// <summary>
        /// Sends a session change message to 
        /// the remote node.
        /// </summary>
        /// <param name="session"></param>
        Task SendSessionAsync(Session session);

        /// <summary>
        /// Receives a session
        /// from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Session> ReceiveSessionAsync(CancellationToken cancellationToken);
    }
}
