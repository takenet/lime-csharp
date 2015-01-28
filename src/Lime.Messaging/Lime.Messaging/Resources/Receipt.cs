using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Resources
{
    /// <summary>
    /// Represents the message events 
    /// that should generate receipts 
    /// (notifications) for the node in the 
    /// current session.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Receipt : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.receipt+json";

        public const string EVENTS_KEY = "events";

        public Receipt()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }

        /// <summary>
        /// Indicates which message events 
        /// that the node is receiving
        /// in the current session.
        /// </summary>
        [DataMember(Name = EVENTS_KEY)]
        public Event[] Events { get; set; }
    }
}
