using System.Collections.Concurrent;
using Lime.Protocol;

namespace Lime.Transport.AspNetCore.Transport
{
    public sealed class ChannelProvider : IChannelProvider
    {
        private readonly ConcurrentDictionary<Node, ISenderChannel> _establishedChannels;
        
        public ChannelProvider()
        {
            _establishedChannels = new ConcurrentDictionary<Node, ISenderChannel>();
        }
        
        public ISenderChannel? GetChannel(Node node) => _establishedChannels.TryGetValue(node, out var c) ? c : null;
        public void AddChannel(Node node, SenderChannelAdapter senderChannel)
        {
            _establishedChannels[node] = senderChannel;
        }
        public void RemoveChannel(Node node)
        {
            _establishedChannels.TryRemove(node, out _);
        }
    }
}