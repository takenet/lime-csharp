using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Server;

namespace Lime.Transport.AspNetCore
{
    public delegate Task<Node> RegistrationHandler(Node node, IServerChannel channel, CancellationToken cancellationToken);
}