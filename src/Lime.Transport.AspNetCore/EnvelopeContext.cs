using System;
using Lime.Protocol;

namespace Lime.Transport.AspNetCore
{
    public abstract class EnvelopeContext
    {
        internal Func<Node, ISenderChannel?> GetChannelFunc = node => throw new InvalidOperationException("Cannot retrieve a channel from here");
        public ISenderChannel? GetChannel(Node node) => GetChannelFunc(node);
        
        /// <summary>
        /// Gets the channel which 
        /// </summary>
        public ISenderChannel Channel { get; internal set; } = null!;
    }
}