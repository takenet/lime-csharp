using System.Runtime.Serialization;

namespace Lime.Protocol.Security
{

    /// <summary>
    /// Defines the valid authentication schemes values.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum AuthenticationScheme
    {
        /// <summary>
        /// The server doesn't requires a client credential, and provides a temporary identity to the node. 
        /// Some restriction may apply to guest sessions,  like the inability of sending some commands or other nodes may want to block messages originated by guest identities.
        /// </summary>
        [EnumMember(Value = "guest")]
        Guest,
        /// <summary>
        /// Username and password authentication.
        /// </summary>
        [EnumMember(Value = "plain")]
        Plain,
        /// <summary>
        /// Transport layer authentication.
        /// </summary>
        [EnumMember(Value = "transport")]
        Transport,
        /// <summary>
        /// Key authentication.
        /// </summary>
        [EnumMember(Value = "key")]
        Key,
        /// <summary>
        /// Third-party authentication.
        /// </summary>
        [EnumMember(Value = "external")]
        External
    }
}
