using Lime.Protocol.Network;
using Lime.Protocol.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        /// Changes the session state and 
        /// sends a negotiate session envelope
        /// to the node with the available 
        /// options
        /// parameters
        /// </summary>
        /// <param name="compressionOptions">The session compression options.</param>
        /// <returns></returns>
        Task SendNegotiatingSessionAsync(SessionCompression[] compressionOptions, SessionEncryption[] encryptionOptions);

        /// <summary>
        /// Changes the session state and 
        /// sends an authenticat envelope
        /// to the node with the available options
        /// </summary>
        /// <param name="authentication">The authentication roundtrip data.</param>
        /// <returns></returns>
        Task SendAuthenticatingSessionAsync(AuthenticationScheme[] schemeOptions);

        /// <summary>
        /// Sends authentication roundtrip information
        /// to the connected node
        /// </summary>
        /// <param name="authenticationRoundtrip">The authentication roundtrip data.</param>
        /// <returns></returns>
        Task SendAuthenticatingSessionAsync(Authentication authenticationRoundtrip);

        /// <summary>
        /// Changes the session state and 
        /// sends a finished session envelope
        /// to the node to comunicate the
        /// end of the session 
        /// </summary>
        Task SendEstablishedSessionAsync(Node node, SessionMode mode);

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

        /// <summary>
        /// Occours when a new session 
        /// request is received by the server
        /// </summary>
        event EventHandler<EnvelopeEventArgs<Session>> NewSessionReceived;

        /// <summary>
        /// Occours when a finish session request
        /// is received by the server
        /// </summary>
        event EventHandler<EnvelopeEventArgs<Session>> FinishingSessionReceived;
    }
}
