using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Server
{
    /// <summary>
    /// Defines a service to register channel nodes instances for the server.
    /// </summary>
    public interface INodeRegistry
    {
        /// <summary>
        /// Try register a node channel. If success, it should return the registered node address; otherwise, returns null.
        /// Note that the returned node instance may be different from the provided in the parameters.
        /// </summary>
        Task<Node> TryRegisterAsync(Node node, IServerChannel channel, CancellationToken cancellationToken);

        /// <summary>
        /// Get a channel for the provided node address.
        /// </summary>
        Task<IServerChannel> GetAsync(Node node, CancellationToken cancellationToken);

        /// <summary>
        /// Unregister a channel for the node. 
        /// </summary>
        Task UnregisterAsync(Node node, CancellationToken cancellationToken);
    }
}