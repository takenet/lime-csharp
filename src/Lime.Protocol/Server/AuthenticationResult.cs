using System;
using Lime.Protocol.Security;

namespace Lime.Protocol.Server
{
    /// <summary>
    /// Represents the result of a session authentication.
    /// </summary>
    public sealed class AuthenticationResult
    {
        public AuthenticationResult(DomainRole domainRole, Identity identity = null, Authentication roundtrip = null)
        {
            DomainRole = domainRole;
            Identity = identity;
            Roundtrip = roundtrip;

            if (domainRole != DomainRole.Unknown && 
                Identity == null)
            {
                throw new ArgumentException("Identity should be supplied if domain role defined", nameof(identity));
            }
        }
        
        /// <summary>
        /// The domain role for the specified node.
        /// </summary>
        public DomainRole DomainRole { get; }
        
        /// <summary>
        /// The authenticated identity.
        /// </summary>
        public Identity Identity { get; }
        
        /// <summary>
        /// The authentication roundtrip.
        /// </summary>
        public Authentication Roundtrip { get; }
    }
}