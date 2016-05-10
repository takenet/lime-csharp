using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using Lime.Protocol;
using Lime.Protocol.Security;

namespace Lime.Messaging.Resources
{
    /// <summary>
    /// Represents the availability status of a node in a network. 
    /// A node can only receive envelopes from another nodes in the network if it sets its presence to an available status (except from the server, who always knows if a node is available or node, since this information is enforced by the existing session). 
    /// In a new session, the node starts with an unavailable status.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    [DebuggerDisplay("Status = {Status}, RoutingRule = {RoutingRule}")]
    public class Presence : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.presence+json";
        public static MediaType MediaType = MediaType.Parse(MIME_TYPE);

        public const string STATUS_KEY = "status";
        public const string MESSAGE_KEY = "message";
        public const string ROUTING_RULE_KEY = "routingRule";
        public const string LAST_SEEN_KEY = "lastSeen";
        public const string PRIORITY_KEY = "priority";
        public const string FILTER_BY_DISTANCE_KEY = "filterByDistance";
        public const string ROUND_ROBIN_KEY = "roundRobin";
        public const string INSTANCES_KEY = "instances";

        /// <summary>
        /// Initializes a new instance of the <see cref="Presence"/> class.
        /// </summary>
        public Presence()
            : base(MediaType)
        {

        }

        /// <summary>
        /// The node presence status.
        /// </summary>
        [DataMember(Name = STATUS_KEY)]
        public PresenceStatus? Status { get; set; }

        /// <summary>
        /// A status message associated to the presence status.
        /// </summary>
        [DataMember(Name = MESSAGE_KEY)]
        public string Message { get; set; }

        /// <summary>
        /// Rule to the server route envelopes addressed to the identity. 
        /// </summary>
        [DataMember(Name = ROUTING_RULE_KEY)]
        public RoutingRule? RoutingRule { get; set; }

        /// <summary>
        /// The date of the last known presence status for the node.
        /// </summary>
        [DataMember(Name = LAST_SEEN_KEY)]
        public DateTimeOffset? LastSeen { get; set; }

        /// <summary>
        /// The value of the priority of the presence.
        /// This value can affect the way the envelopes are routed to the current session.
        /// </summary>
        [DataMember(Name = PRIORITY_KEY, EmitDefaultValue = false)]
        public int Priority { get; set; }

        /// <summary>
        /// If true, indicates that the delivery of envelopes for the current session should only occurs if the distance from the originator is the smallest among the resolved routes with this setting. 
        /// This configuration is not exclusive, so if there's more than one route with the same smallest distance, these sessions should receive the envelopes.
        /// </summary>
        [DataMember(Name = FILTER_BY_DISTANCE_KEY, EmitDefaultValue = false)]
        public bool? FilterByDistance { get; set; }

        /// <summary>
        /// If true, indicates that the delivery of envelopes for the current session should be distributed by using the round-robin strategy between the resolved routes with this setting.
        /// </summary>
        [DataMember(Name = ROUND_ROBIN_KEY, EmitDefaultValue = false)]
        public bool? RoundRobin { get; set; }

        /// <summary>
        /// Present instances for a identity.
        /// </summary>
        [DataMember(Name = INSTANCES_KEY)]
        public string[] Instances { get; set; }
    }

    /// <summary>
    /// Defines the envelope routing rules.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum RoutingRule
    {
        /// <summary>
        /// Only deliver envelopes addressed to the current session instance (name@domain/instance).
        /// </summary>
        [EnumMember(Value = "instance")]
        Instance,
        /// <summary>
        /// Deliver envelopes addressed to the current session instance (name@domain/instance) and to the identity (name@domain).
        /// </summary>
        [EnumMember(Value = "identity")]
        Identity,
        /// <summary>
        /// Deliver envelopes addressed to the current session instance (name@domain/instance) and to the identity (name@domain) 
        /// if the distance from the originator is the smallest among the available nodes of the identity with this setting.
        /// </summary>
        [Obsolete("Use presence's 'FilterByDistance' property instead")]
        [EnumMember(Value = "identityByDistance")]
        IdentityByDistance,
        /// <summary>
        /// Deliver any envelopes addressed to the identity name@domain, including the envelopes addressed to any specific instance.
        /// </summary>
        [EnumMember(Value = "promiscuous")]
        Promiscuous,
        /// <summary>
        /// Deliver envelopes addressed to the current session instance (name@domain/instance) and to the node domain.
        /// This rule is intended to be used only by sessions authenticated as <see cref="DomainRole.Authority"/>.
        /// </summary>
        [EnumMember(Value = "domain")]
        Domain,
        /// <summary>
        /// Deliver envelopes addressed to the current session  instance (name@domain/instance) and to the node domain
        /// if the distance from the originator is the smallest among the available  nodes of the domain with this setting.
        /// This rule is intended to be used only by sessions authenticated as <see cref="DomainRole.Authority"/>.
        /// </summary>
        [Obsolete("Use presence's 'FilterByDistance' property instead")]
        [EnumMember(Value = "domainByDistance")]
        DomainByDistance,

        /// <summary>
        /// Deliver envelopes addressed to the current session instance (name@domain/instance), to the node domain and all its sub domains.
        /// This rule is intended to be used only by sessions authenticated as <see cref="DomainRole.RootAuthority"/>.
        /// </summary>
        [EnumMember(Value = "rootDomain")]
        RootDomain
    }

    /// <summary>
    /// Possible presence status values
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum PresenceStatus
    {
        /// <summary>
        /// The node is not available for messaging and should not receive any envelope by any node, except by the connected server.
        /// </summary>
        [EnumMember(Value = "unavailable")]
        Unavailable,
        /// <summary>
        /// The node is available for messaging and envelopes can be routed to the node according to the defined routing rule.
        /// </summary>
        [EnumMember(Value = "available")]
        Available,
        /// <summary>
        /// The node is available but the senders should notice that it is busy and doesn't want to the disturbed 
        /// or it is on heavy load and don't want to receive any envelope.
        /// </summary>
        [EnumMember(Value = "busy")]
        Busy,
        /// <summary>
        /// The node is available but the senders should notice that it may not be reading or processing the received envelopes.
        /// </summary>
        [EnumMember(Value = "away")]
        Away,
        /// <summary>
        /// The node is available for messaging but the actual stored presence value is unavailable.
        /// </summary>
        [EnumMember(Value = "invisible")]
        Invisible
    }
}
