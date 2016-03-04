using Lime.Protocol.Network;
using Lime.Protocol.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Server
{
    /// <summary>
    /// Defines the communication channel
    /// between a server and a node
    /// </summary>
    public interface IServerChannel : IChannel
    {
        /// <summary>
        /// Receives a new session envelope
        /// from the client node.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Session> ReceiveNewSessionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Changes the session state and sends a negotiate session envelope to the node with the available options and awaits for the client selected option.
        /// </summary>
        /// <param name="compressionOptions">The session compression options.</param>
        /// <param name="encryptionOptions">The encryption options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A negotiating session envelope with the client node selected options.
        /// </returns>
        Task<Session> NegotiateSessionAsync(SessionCompression[] compressionOptions, SessionEncryption[] encryptionOptions, CancellationToken cancellationToken);

        /// <summary>
        /// Send a negotiate session envelope to confirm the session negotiation options.
        /// </summary>
        /// <param name="sessionCompression">The session compression option</param>
        /// <param name="sessionEncryption">The session encryption option</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendNegotiatingSessionAsync(SessionCompression sessionCompression, SessionEncryption sessionEncryption, CancellationToken cancellationToken);

        /// <summary>
        /// Changes the session state and sends an authenticating envelope to the node with the available options and awaits for the client authentication.
        /// </summary>
        /// <param name="schemeOptions">The scheme options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A authenticating session envelope with the authentication information.
        /// </returns>
        Task<Session> AuthenticateSessionAsync(AuthenticationScheme[] schemeOptions, CancellationToken cancellationToken);

        /// <summary>
        /// Sends authentication round-trip information to the connected node and awaits for the client authentication.
        /// </summary>
        /// <param name="authenticationRoundtrip">The authentication round-trip data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A authenticating session envelope with the authentication information.
        /// </returns>
        Task<Session> AuthenticateSessionAsync(Authentication authenticationRoundtrip, CancellationToken cancellationToken);

        /// <summary>
        /// Changes the session state and sends a finished session envelope to the node to communicate the end of the session.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SendEstablishedSessionAsync(Node node, CancellationToken cancellationToken);

        /// <summary>
        /// Receives a finishing session envelope from the client node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Session> ReceiveFinishingSessionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Changes the session state and sends a finished session envelope to the node to communicate the end of the session and closes the transport.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SendFinishedSessionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Changes the session state and sends a failed session envelope to the node to communicate the finished session and closes the transport.
        /// </summary>
        /// <param name="reason">The reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SendFailedSessionAsync(Reason reason, CancellationToken cancellationToken);
       
    }
}
