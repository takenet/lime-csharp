using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a channel to exchange session envelopes.
    /// </summary>
    public interface ISessionChannel : ISessionSenderChannel, ISessionReceiverChannel
    {

    }

    /// <summary>
    /// Defines a channel to send session envelopes.
    /// </summary>
    public interface ISessionSenderChannel
    {
        /// <summary>
        /// Sends a session to the remote node.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendSessionAsync(Session session, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Defines a channel to receive session envelopes.
    /// </summary>
    public interface ISessionReceiverChannel
    {
        /// <summary>
        /// Receives a session from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Session> ReceiveSessionAsync(CancellationToken cancellationToken);
    }
}
