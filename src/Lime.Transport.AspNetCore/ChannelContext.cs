using System;
using Lime.Protocol;
using Lime.Transport.AspNetCore.Transport;

namespace Lime.Transport.AspNetCore
{
    /// <summary>
    /// Provide context information about the connected channels.
    /// </summary>
    public sealed class ChannelContext
    {
        private readonly IChannelProvider _channelProvider;

        public ChannelContext(ISenderChannel channel, IChannelProvider channelProvider)
        {
            Channel = channel;
            _channelProvider = channelProvider;
        }

        /// <summary>
        /// Gets the channel which the current envelope was received from.
        /// </summary>
        public ISenderChannel Channel { get; }
        
        /// <summary>
        /// Gets a channel for the specified remote note.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public ISenderChannel? GetChannel(Node node) => _channelProvider.GetChannel(node);
    }
}