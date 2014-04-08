using Lime.Protocol.Network;
using Lime.Protocol.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Client
{
    /// <summary>
    /// Defines the communication channel
    /// between a node and a server
    /// </summary>
    public interface IClientChannel : IChannel
    {
        /// <summary>
        /// Sends a new session envelope
        /// to the server and awaits for
        /// the response.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An negotiating session envelope, 
        /// an authenticating session envelope (if there's no need for negotiation) 
        /// or a failed session envelope.</returns>
        Task<Session> StartNewSessionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Sends a negotiate session envelope
        /// to accepts the session negotiation options
        /// and awaits for the server confirmation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="sessionCompression">The session compression option</param>
        /// <param name="sessionEncryption">The session encryption option</param>
        /// <returns>An negotiating session envelope or a failed session envelope.</returns>
        Task<Session> NegotiateSessionAsync(CancellationToken cancellationToken, SessionCompression sessionCompression, SessionEncryption sessionEncryption);

        /// <summary>
        /// Sends a authenticate session envelope
        /// to the server to establish
        /// an authenticated session and awaits
        /// for the established session envelope.
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="authentication"></param>
        /// <param name="instance"></param>
        /// <param name="sessionMode"></param>
        /// <returns>An established session envelope or a failed session envelope.</returns>
        Task<Session> AuthenticateSessionAsync(CancellationToken cancellationToken, Identity identity, Authentication authentication, string instance = null, SessionMode sessionMode = SessionMode.Node);

        /// <summary>
        /// Notify to the server that
        /// the specified message was received
        /// by the peer
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="to">The notification destination.</param>
        /// <returns></returns>        
        Task SendReceivedNotificationAsync(Guid messageId, Node to);

        /// <summary>
        /// Sends a finishing session 
        /// envelope to the server.
        /// </summary>
        Task SendFinishingSessionAsync();

        /// <summary>
        /// Receives a finished session envelope
        /// from the server. 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>An finished session envelope or a failed session envelope.</returns>
        Task<Session> ReceiveSessionFinishedAsync(CancellationToken cancellationToken); 
    }
}