using System;
using Lime.Protocol.Security;

namespace Lime.Protocol.Server
{
    /// <summary>
    /// Represents the result of a session authentication.
    /// </summary>
    public sealed class AuthenticationResult
    {
        public AuthenticationResult(DomainRole domainRole, Node node = null, Authentication roundtrip = null)
        {
            DomainRole = domainRole;
            Node = node;
            Roundtrip = roundtrip;

            if (domainRole != DomainRole.Unknown && 
                Node == null)
            {
                throw new ArgumentException("Node should be supplied if domain role defined", nameof(node));
            }
        }
        
        /// <summary>
        /// The domain role for the specified node.
        /// </summary>
        public DomainRole DomainRole { get; }
        
        /// <summary>
        /// The node to be registered.
        /// </summary>
        public Node Node { get; }
        
        /// <summary>
        /// The authentication roundtrip.
        /// </summary>
        public Authentication Roundtrip { get; }
    }
}