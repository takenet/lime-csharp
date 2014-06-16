using Lime.Protocol.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Security
{
    /// <summary>
    /// Defines a transport that
    /// supports authentication
    /// </summary>
    public interface IAuthenticatableTransport : ITransport
    {
        /// <summary>
        /// Authenticate the identity
        /// in the transport layer
        /// </summary>
        /// <param name="identity">The identity to be authenticated</param>
        /// <returns>Indicates if the identity is authenticated</returns>
        Task<bool> AuthenticateAsync(Identity identity);
    }
}
