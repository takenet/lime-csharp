using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Resources
{
    /// <summary>
    /// Represents a delegation to send envelopes on behalf of another node. 
    /// The delegation can be constrained to specific envelope types and specific details.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Delegation : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.delegation+json";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        public const string TARGET_KEY = "target";
        public const string ENVELOPE_TYPES_KEY = "envelopeTypes";
        public const string MESSAGES_KEY = "messages";
        public const string NOTIFICATIONS_KEY = "notifications";
        public const string COMMANDS_KEY = "commands";
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Delegation"/> class.
        /// </summary>
        public Delegation()
            : base(MediaType)
        {

        }

        /// <summary>
        /// Gets or sets the target node which will receive the delegation.
        /// </summary>
        /// <value>
        /// The target.
        /// </value>
        [DataMember(Name = TARGET_KEY)]
        public Node Target { get; set; }

        /// <summary>
        /// Gets or sets the envelope types for delegation.
        /// If none is specified, indicates that all envelope types should be delegated.
        /// </summary>
        /// <value>
        /// The envelope types.
        /// </value>
        [DataMember(Name = ENVELOPE_TYPES_KEY)]
        public EnvelopeType[] EnvelopeTypes { get; set; }

        /// <summary>
        /// Message constraints for delegation. 
        /// If not present, the delegation is given without any restriction for the <see cref="Message"/> envelope type.
        /// </summary>
        [DataMember(Name = MESSAGES_KEY)]
        public DelegationMessage[] Messages { get; set; }

        /// <summary>
        /// Notification constraints for delegation. 
        /// If not present, the delegation is given without any restriction for the <see cref="Notification"/> envelope type.
        /// </summary>
        [DataMember(Name = NOTIFICATIONS_KEY)]
        public DelegationNotification[] Notifications { get; set; }

        /// <summary>
        /// Command constraints for delegation. 
        /// If not present, the delegation is given without any restriction for the <see cref="Command"/> envelope type.
        /// </summary>
        [DataMember(Name = COMMANDS_KEY)]
        public DelegationCommand[] Commands { get; set; }
    }

    /// <summary>
    /// Represents <see cref="Message"/> delegation constraints.
    /// </summary>
    [DataContract]
    public class DelegationMessage
    {
        public const string TYPE_KEY = "type";

        /// <summary>
        /// Gets or sets the message type for delegation.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [DataMember(Name = TYPE_KEY)]
        public MediaType Type { get; set; }
    }

    /// <summary>
    /// Represents <see cref="Notification"/> delegation constraints.
    /// </summary>
    [DataContract]
    public class DelegationNotification
    {
        public const string EVENT_KEY = "event";

        /// <summary>
        /// Gets or sets the event type for delegation.
        /// </summary>
        /// <value>
        /// The event.
        /// </value>
        [DataMember(Name = EVENT_KEY)]
        public Event Event { get; set; }
    }

    /// <summary>
    /// Represents <see cref="Command"/> delegation constraints.
    /// </summary>
    [DataContract]
    public class DelegationCommand 
    {
        public const string METHOD_KEY = "method";
        public const string URI_KEY = "uri";        
        public const string STATUS_KEY = "status";

        /// <summary>
        /// Gets or sets the method for delegation.
        /// If not present, The delegation is given for any method in the specified URI.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        [DataMember(Name = METHOD_KEY)]
        public CommandMethod? Method { get; set; }

        /// <summary>
        /// Gets or sets the resource URI fo delegation.
        /// </summary>
        /// <value>
        /// The URI.
        /// </value>
        [DataMember(Name = URI_KEY)]
        public LimeUri Uri { get; set; }

        /// <summary>
        /// Gets or sets the comand status for delegation.
        /// If not present, The delegation is given for any status in the specified URI.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        [DataMember(Name = STATUS_KEY)]
        public CommandStatus? Status { get; set; }
    }
}
