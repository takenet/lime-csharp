using System;
using Lime.Protocol;

namespace Lime.Transport.AspNetCore
{
    /// <summary>
    /// Provide context information about the connected channels.
    /// </summary>
    public sealed class ChannelContext
    {
        private readonly Func<Node, ISenderChannel?> _getChannelFunc;

        public ChannelContext(ISenderChannel channel, Func<Node, ISenderChannel?> getChannelFunc)
        {
            Channel = channel;
            _getChannelFunc = getChannelFunc;
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
        public ISenderChannel? GetChannel(Node node) => _getChannelFunc(node);
    }
}