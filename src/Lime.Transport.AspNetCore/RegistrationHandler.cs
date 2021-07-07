using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Server;

namespace Lime.Transport.AspNetCore
{
    /// <summary>
    /// Defines a handler for registering a node. The implementation should return a node address for the specified candidate node. 
    /// </summary>
    public delegate Task<Node> RegistrationHandler(Node candidate, IServerChannel channel, CancellationToken cancellationToken);
}