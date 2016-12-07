using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Resources
{
    /// <summary>
    /// Represents the events that should generate receipts (notifications) for the messages sent by the owner identity.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Receipt : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.receipt+json";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        public const string EVENTS_KEY = "events";
        public const string DELIVER_HOPS_KEY = "deliverHops";

        /// <summary>
        /// Initializes a new instance of the <see cref="Receipt"/> class.
        /// </summary>
        public Receipt()
            : base(MediaType)
        {

        }

        /// <summary>
        /// Indicates which message events that the identity want to receive.
        /// </summary>
        [DataMember(Name = EVENTS_KEY)]
        public Event[] Events { get; set; }

        /// <summary>
        /// Indicates if the events sent by nodes between the message originator and destination (server, gateways) should be delivered.
        /// </summary>
        [DataMember(Name = DELIVER_HOPS_KEY)]
        public bool? DeliverHops { get; set; }
    }
}
