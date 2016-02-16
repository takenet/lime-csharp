using System;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Provides information about a channel.
    /// </summary>
    public interface IChannelInformation
    {
        /// <summary>
        /// Gets the channel session identifier.
        /// </summary>
        Guid SessionId { get; }

        /// <summary>
        /// Gets the channel state.
        /// </summary>
        SessionState State { get; }

        /// <summary>
        /// Gets the channel local node.
        /// </summary>
        Node LocalNode { get; }

        /// <summary>
        /// Gets the channel remote node.
        /// </summary>
        Node RemoteNode { get; }
    }
}