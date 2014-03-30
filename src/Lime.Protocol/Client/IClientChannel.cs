using Lime.Protocol.Network;
using Lime.Protocol.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        /// to the server to start a
        /// session negotiation
        /// </summary>
        /// <returns></returns>
        Task SendNewSessionAsync();

        /// <summary>
        /// Send a authenticate session envelope
        /// to the server to establish
        /// an authenticated session
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="authentication">Authentication information.</param>
        /// <param name="instance"></param>
        /// <param name="sessionMode"></param>
        /// <returns></returns>
        Task SendAuthenticatingSessionAsync(Identity identity, Authentication authentication, string instance = null, SessionMode sessionMode = SessionMode.Node);

        /// <summary>
        /// Sends a finish session envelope
        /// to the server to finish the session
        /// </summary>
        Task SendFinishingSessionAsync();

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
        /// Occurs when the session is established
        /// with the server
        /// </summary>
        event EventHandler<EnvelopeEventArgs<Session>> SessionEstablished;

        /// <summary>
        /// Occurs when the session fails
        /// with the server
        /// </summary>
        event EventHandler<EnvelopeEventArgs<Session>> SessionFailed;

        /// <summary>
        /// Occurs when the session ends
        /// with the server
        /// </summary>
        event EventHandler<EnvelopeEventArgs<Session>> SessionFinished;
    }
}
