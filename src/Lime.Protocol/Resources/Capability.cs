using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Resources
{
    /// <summary>
    /// Represents the capabilities
    /// of the nodes of the network
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Capability : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.capability+json";

        public Capability()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }

        /// <summary>
        /// Indicates the message content types
        /// that the session node is able to handle.
        /// </summary>
        [DataMember(Name = "contentTypes")]
        public MediaType[] ContentTypes { get; set; }

        /// <summary>
        /// Indicates the command resource types 
        /// that the session node is able to handle.
        /// </summary>
        [DataMember(Name = "resourceTypes")]
        public MediaType[] ResourceTypes { get; set; }
    }
}