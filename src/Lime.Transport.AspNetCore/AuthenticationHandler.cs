using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Security;
using Lime.Protocol.Server;

namespace Lime.Transport.AspNetCore
{
    /// <summary>
    /// Defines a handler for authenticating the an identity.
    /// </summary>
    public delegate Task<AuthenticationResult> AuthenticationHandler(Identity identity, Authentication authentication, CancellationToken cancellationToken);
}