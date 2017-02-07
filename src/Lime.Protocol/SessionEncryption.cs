using System;
using System.Runtime.Serialization;
using Lime.Protocol.Security;

namespace Lime.Protocol
{

    /// <summary>
    /// Defines the valid session encryption values.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum SessionEncryption
    {
        /// <summary>
        /// The session is not encrypted.
        /// </summary>
        [EnumMember(Value = "none")]
        None,
        /// <summary>
        /// The session is encrypted by TLS 
        /// (Transport Layer Security).
        /// </summary>
        [EnumMember(Value = "tls")]
        TLS
    }
}
