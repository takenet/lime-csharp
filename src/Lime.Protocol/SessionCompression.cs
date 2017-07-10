using System;
using System.Runtime.Serialization;
using Lime.Protocol.Security;

namespace Lime.Protocol
{

    /// <summary>
    /// Defines the valid session compression values.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum SessionCompression
    {
        /// <summary>
        /// The session is not compressed.
        /// </summary>
        [EnumMember(Value = "none")]
        None,
        /// <summary>
        /// The session is using the GZip 
        /// algorithm for compression.
        /// </summary>
        [EnumMember(Value = "gzip")]        
        GZip
    }
}
