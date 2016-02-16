using System;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Provides information about a channel.
    /// </summary>
    public class ChannelInformation : IChannelInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelInformation"/> class.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="state">The state.</param>
        /// <param name="localNode">The channel local node.</param>
        /// <param name="remoteNode">The channel remote node.</param>
        public ChannelInformation(Guid sessionId, SessionState state, Node localNode, Node remoteNode)
        {
            SessionId = sessionId;
            State = state;
            LocalNode = localNode;
            RemoteNode = remoteNode;
        }

        /// <summary>
        /// Gets the associated channel session identifier.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Gets the channel state.
        /// </summary>
        public SessionState State { get; }

        /// <summary>
        /// Gets the channel local node.
        /// </summary>
        public Node LocalNode { get; }

        /// <summary>
        /// Gets the channel remote node.
        /// </summary>
        public Node RemoteNode { get; }
    }
}