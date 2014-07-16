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
        /// Changes the session state and 
        /// sends a negotiate session envelope
        /// to the node with the available 
        /// options and awaits for the client
        /// selected option.
        /// </summary>
        /// <param name="compressionOptions">The session compression options.</param>
        /// <returns>A negotiating session envelope with the client node selected options.</returns>
        Task<Session> NegotiateSessionAsync(SessionCompression[] compressionOptions, SessionEncryption[] encryptionOptions, CancellationToken cancellationToken);

        /// <summary>
        /// Send a negotiate session envelope
        /// to confirm the session negotiation options.
        /// </summary>
        /// <param name="sessionCompression">The session compression option</param>
        /// <param name="sessionEncryption">The session encryption option</param>
        /// <returns></returns>
        Task SendNegotiatingSessionAsync(SessionCompression sessionCompression, SessionEncryption sessionEncryption);

        /// <summary>
        /// Changes the session state and 
        /// sends an authenticat envelope
        /// to the node with the available options 
        /// and awaits for the client authentication.
        /// </summary>
        /// <param name="authentication">The authentication roundtrip data.</param>
        /// <returns>A autheticating session envelope with the authentication information.</returns>
        Task<Session> AuthenticateSessionAsync(AuthenticationScheme[] schemeOptions, CancellationToken cancellationToken);

        /// <summary>
        /// Sends authentication roundtrip information
        /// to the connected node and awaits
        /// for the client authentication.
        /// </summary>
        /// <param name="authenticationRoundtrip">The authentication roundtrip data.</param>
        /// <returns>A autheticating session envelope with the authentication information.</returns>
        Task<Session> AuthenticateSessionAsync(Authentication authenticationRoundtrip, CancellationToken cancellationToken);

        /// <summary>
        /// Changes the session state and 
        /// sends a finished session envelope
        /// to the node to comunicate the
        /// end of the session 
        /// </summary>
        Task SendEstablishedSessionAsync(Node node);

        /// <summary>
        /// Receives a finishing session envelope
        /// from the client node.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Session> ReceiveFinishingSessionAsync(CancellationToken cancellationToken);
                
        /// <summary>
        /// Changes the session state and 
        /// sends a finished session envelope
        /// to the node to comunicate the
        /// end of the session and closes
        /// the transport
        /// </summary>
        Task SendFinishedSessionAsync();

        /// <summary>
        /// Changes the session state and 
        /// sends a failed session envelope
        /// to the node to comunicate the
        /// finished session and closes
        /// the transport
        /// </summary>
        Task SendFailedSessionAsync(Reason reason);
       
    }
}
