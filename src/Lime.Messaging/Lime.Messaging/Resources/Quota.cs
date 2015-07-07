using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Resources
{
    /// <summary>
    /// Allows the nodes to manage the 
    /// session envelope quota configuration.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Quota : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.quota+json";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="Quota"/> class.
        /// </summary>
        public Quota()
            : base(MediaType)
        {

        }

        /// <summary>
        /// The number of envelopes that the node
        /// can originate in the current session.       
        /// If the value is 0 or is
        /// not defined, this limit is unbounded. 
        /// If a session exceeds the limit, it can be finished by the server.
        /// </summary>
        [DataMember(Name = "threshold")]
        public int? Threshold { get; set; }

        /// <summary>
        /// Indicates the cap of envelopes per second that the 
        /// session can send and receive. If the value is 0 or is
        /// not defined, this capability is unbounded. 
        /// If a session exceeds the limit, it can be finished by the server.
        /// </summary>
        [DataMember(Name = "throughput")]
        public int? Throughput { get; set; }

        /// <summary>
        /// The uncompressed size limit of the envelopes that 
        /// can be sent in the session. If the value is 0 or is 
        /// not defined, this capability is unbounded. 
        /// If a envelope sent in the session exceeds the limit, it can be finished 
        /// by the server.
        /// </summary>
        [DataMember(Name = "maxEnvelopeSize")]
        public int? MaxEnvelopeSize { get; set; }
    }
}