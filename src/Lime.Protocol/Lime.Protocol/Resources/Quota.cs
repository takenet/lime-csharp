using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Resources
{
    /// <summary>
    /// Allows the nodes to manage the 
    /// session envelope quota configuration.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public partial class Quota : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.quota+json";

        public Quota()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }

        /// <summary>
        /// Indicates the cap of envelopes per second that the 
        /// session can send and receive. If the value is 0 or is
        /// not defined, this capability is unbounded. If a session 
        /// exceeds the limit, it will be finished by the server.
        /// Usually, only the server is allowed to change this 
        /// property in a session.
        /// </summary>
        [DataMember(Name = "throughput")]
        public int Throughput { get; set; }

        /// <summary>
        /// The uncompressed size limit of the envelopes that 
        /// can be sent in the session. If the value is 0 or is 
        /// not defined, this capability is unbounded. If a envelope 
        /// sent in the session exceeds the limit, it will be finished 
        /// by the server. Usually, only the server is allowed to 
        /// change this property in a session.
        /// </summary>
        [DataMember(Name = "maxEnvelopeSize")]
        public int MaxEnvelopeSize { get; set; }
    }
}
