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
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        public const string EVENTS_KEY = "events";

        /// <summary>
        /// Initializes a new instance of the <see cref="Receipt"/> class.
        /// </summary>
        public Receipt()
            : base(MediaType)
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
