using System;
using Lime.Protocol.Security;

namespace Lime.Protocol.Server
{
    /// <summary>
    /// Represents the result of a session authentication.
    /// </summary>
    public sealed class AuthenticationResult
    {
        public AuthenticationResult(DomainRole domainRole, Authentication roundtrip = null)
        {
            DomainRole = domainRole;
            Roundtrip = roundtrip;
        }
        
        /// <summary>
        /// The domain role for the specified node.
        /// </summary>
        public DomainRole DomainRole { get; }

        /// <summary>
        /// The authentication roundtrip.
        /// </summary>
        public Authentication Roundtrip { get; }
    }
}