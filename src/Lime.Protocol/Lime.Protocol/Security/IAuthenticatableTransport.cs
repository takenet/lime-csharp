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
        Task<DomainRole> AuthenticateAsync(Identity identity);
    }

    /// <summary>
    /// Indicates the role of
    /// an identity in a domain
    /// </summary>
    public enum DomainRole
    {
        /// <summary>
        /// The identity is unknown
        /// in the domain
        /// </summary>
        Unknown,
        /// <summary>
        /// The identity is a 
        /// member of the domain
        /// </summary>
        Member,
        /// <summary>
        /// The identity is a
        /// domain authority
        /// </summary>
        Authority
    }
}