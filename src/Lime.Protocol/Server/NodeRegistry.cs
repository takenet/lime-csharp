using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Server
{
    /// <summary>
    /// Implements a simple in-memory <see cref="INodeRegistry"/>.
    /// </summary>
    public sealed class NodeRegistry : INodeRegistry
    {
        private readonly ConcurrentDictionary<Node, IServerChannel> _nodeChannelsDictionary;
        
        public NodeRegistry()
        {
            _nodeChannelsDictionary = new ConcurrentDictionary<Node, IServerChannel>();
        }
        
        public Task<Node> TryRegisterAsync(Node node, IServerChannel channel, CancellationToken cancellationToken)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (node.Name == null || node.Domain == null)
            {
                return Task.FromResult<Node>(null);
            }
            var copiedNode = node.Copy();
            copiedNode.Instance ??= "default";
            return _nodeChannelsDictionary.TryAdd(copiedNode, channel) 
                ? Task.FromResult(copiedNode) 
                : Task.FromResult<Node>(null);
        }

        public Task<IServerChannel> GetAsync(Node node, CancellationToken cancellationToken)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            _nodeChannelsDictionary.TryGetValue(node, out var channel);
            return Task.FromResult(channel);
        }

        public Task UnregisterAsync(Node node, CancellationToken cancellationToken)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            _nodeChannelsDictionary.TryRemove(node, out _);
            return Task.CompletedTask;
        }
    }
}