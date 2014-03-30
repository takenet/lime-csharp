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

        /// <summary>
        /// Current session mode
        /// </summary>
        SessionMode Mode { get; }

        /// <summary>
        /// Send a negotiate session envelope
        /// to the remote node to choose or
        /// confirm the session negotiation options
        /// parameters
        /// </summary>
        /// <param name="sessionCompression">The session compression option</param>
        /// <param name="sessionEncryption">The session encryption option</param>
        /// <returns></returns>
        Task SendNegotiatingSessionAsync(SessionCompression sessionCompression, SessionEncryption sessionEncryption);

        /// <summary>
        /// Occours when a session negotiation option
        /// is received by the node or server
        /// </summary>
        event EventHandler<EnvelopeEventArgs<Session>> NegotiateSessionReceived;

        /// <summary>
        /// Occours when a session authentication option
        /// is received by the node or server
        /// </summary>
        event EventHandler<EnvelopeEventArgs<Session>> AuthenticateSessionReceived;
    }
}
