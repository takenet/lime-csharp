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
    
    internal class ChannelContextProvider
    {
        private ChannelContext? _context;
        private bool _used;

        public void SetContext(ChannelContext requestContext)
        {
            if (_context != null || requestContext == null)
            {
                throw new InvalidOperationException();
            }

            _context = requestContext;
        }

        public ChannelContext GetContext()
        {
            if (_used || _context == null)
            {
                throw new InvalidOperationException();
            }

            _used = true;
            return _context;
        }
    }
}