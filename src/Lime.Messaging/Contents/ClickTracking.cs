using Lime.Protocol;
using System.Runtime.Serialization;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents a click tracking event, used to record link interactions from broadcast messages.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class ClickTracking : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.click-tracking+json";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        public const string FROM_IDENTITY_KEY = "fromIdentity";
        public const string TO_IDENTITY_KEY = "toIdentity";
        public const string MESSAGE_ID_KEY = "messageId";
        public const string FROM_DOMAIN_KEY = "fromDomain";
        public const string TO_DOMAIN_KEY = "toDomain";
        public const string STORAGE_DATE_BR_KEY = "storageDateBR";
        public const string STORAGE_DATE_DAY_BR_KEY = "storageDateDayBR";
        public const string IS_TAKE_SIGNATURE_KEY = "isTakeSignature";
        public const string METADATA_KEY = "metadata";

        /// <summary>
        /// Initializes a new instance of the <see cref="ClickTracking"/> class.
        /// </summary>
        public ClickTracking()
            : base(MediaType)
        {
        }

        /// <summary>
        /// Gets or sets the identity of the user (contact) who performed the click (BSUID).
        /// </summary>
        [DataMember(Name = FROM_IDENTITY_KEY)]
        public string FromIdentity { get; set; }

        /// <summary>
        /// Gets or sets the identity of the bot that sent the original broadcast message.
        /// </summary>
        [DataMember(Name = TO_IDENTITY_KEY)]
        public string ToIdentity { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the original broadcast message that was clicked.
        /// </summary>
        [DataMember(Name = MESSAGE_ID_KEY)]
        public string MessageId { get; set; }

        /// <summary>
        /// Gets or sets the source channel domain (e.g. wa.gw, google.rcs).
        /// </summary>
        [DataMember(Name = FROM_DOMAIN_KEY)]
        public string FromDomain { get; set; }

        /// <summary>
        /// Gets or sets the destination channel domain (e.g. wa.gw, google.rcs).
        /// </summary>
        [DataMember(Name = TO_DOMAIN_KEY)]
        public string ToDomain { get; set; }

        /// <summary>
        /// Gets or sets the localized Brazilian date and time of the interaction.
        /// </summary>
        [DataMember(Name = STORAGE_DATE_BR_KEY)]
        public string StorageDateBR { get; set; }

        /// <summary>
        /// Gets or sets the localized Brazilian date (day only) of the interaction.
        /// </summary>
        [DataMember(Name = STORAGE_DATE_DAY_BR_KEY)]
        public string StorageDateDayBR { get; set; }

        /// <summary>
        /// Gets or sets whether the message originated from a Take infrastructure node.
        /// </summary>
        [DataMember(Name = IS_TAKE_SIGNATURE_KEY)]
        public bool? IsTakeSignature { get; set; }

        /// <summary>
        /// Gets or sets the tracking metadata associated with the click event.
        /// </summary>
        [DataMember(Name = METADATA_KEY)]
        public ClickTrackingMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Represents the tracking metadata of a <see cref="ClickTracking"/> event.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class ClickTrackingMetadata
    {
        public const string TRACKING_URL_ID_KEY = "trackingUrlId";
        public const string TEMPLATE_ID_KEY = "templateId";
        public const string TRACKING_TYPE_KEY = "trackingType";
        public const string EVENT_TYPE_KEY = "eventType";
        public const string CLIENT_PARAM_KEY = "clientParam";
        public const string CORRELATION_ID_KEY = "correlationId";
        public const string USER_AGENT_KEY = "userAgent";
        public const string IP_ADDRESS_KEY = "ipAddress";
        public const string TENANT_KEY = "tenant";

        /// <summary>
        /// Gets or sets the identifier of the tracking URL that was accessed.
        /// </summary>
        [DataMember(Name = TRACKING_URL_ID_KEY)]
        public string TrackingUrlId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the broadcast template.
        /// </summary>
        [DataMember(Name = TEMPLATE_ID_KEY)]
        public string TemplateId { get; set; }

        /// <summary>
        /// Gets or sets the type of tracking (e.g. the tracking method or category).
        /// </summary>
        [DataMember(Name = TRACKING_TYPE_KEY)]
        public string TrackingType { get; set; }

        /// <summary>
        /// Gets or sets the event type that triggered the tracking (e.g. "url_access").
        /// </summary>
        [DataMember(Name = EVENT_TYPE_KEY)]
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets additional client-specific parameters sent with the click.
        /// </summary>
        [DataMember(Name = CLIENT_PARAM_KEY)]
        public string ClientParam { get; set; }

        /// <summary>
        /// Gets or sets the correlation identifier linking this event to a broader flow.
        /// </summary>
        [DataMember(Name = CORRELATION_ID_KEY)]
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the user agent string of the client that performed the click.
        /// </summary>
        [DataMember(Name = USER_AGENT_KEY)]
        public string UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the client that performed the click.
        /// </summary>
        [DataMember(Name = IP_ADDRESS_KEY)]
        public string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the tenant (contract) identifier associated with the interaction.
        /// </summary>
        [DataMember(Name = TENANT_KEY)]
        public string Tenant { get; set; }
    }
}
