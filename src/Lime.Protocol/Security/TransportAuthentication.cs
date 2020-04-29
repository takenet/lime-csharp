using System.Runtime.Serialization;

namespace Lime.Protocol.Security
{
    /// <summary>
    /// Defines a transport layer authentication scheme. 
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class TransportAuthentication : Authentication
    {
        public TransportAuthentication()
            : base(AuthenticationScheme.Transport)
        {
        }

        /// <summary>
        /// The domain role determined by the <see cref="IAuthenticatableTransport.AuthenticateAsync"/> method call.
        /// This value should not be serialized.
        /// </summary>
        [IgnoreDataMember]
        public DomainRole? DomainRole { get; set; }
    }
}
