using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Resources
{
    /// <summary>
    /// Represents the capabilities
    /// of the nodes of the network
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Capability : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.capability+json";

        public const string CONTENT_TYPES_KEY = "contentTypes";
        public const string RESOURCE_TYPES_KEY = "resourceTypes";

        public Capability()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }

        /// <summary>
        /// Indicates the message content types
        /// that the session node is able to handle.
        /// </summary>
        [DataMember(Name = CONTENT_TYPES_KEY)]
        public MediaType[] ContentTypes { get; set; }

        /// <summary>
        /// Indicates the command resource types 
        /// that the session node is able to handle.
        /// </summary>
        [DataMember(Name = RESOURCE_TYPES_KEY)]
        public MediaType[] ResourceTypes { get; set; }
    }
}