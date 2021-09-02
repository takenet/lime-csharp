using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Server;

namespace Lime.Transport.AspNetCore
{
    /// <summary>
    /// Defines a handler for unregistering a node.  
    /// </summary>
    public delegate Task UnregistrationHandler(Node node, IServerChannel channel, CancellationToken cancellationToken);
}