using Lime.Protocol;
using System;
using System.Runtime.Serialization;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents a click tracking event, used to record link interactions from broadcast messages.
    /// Aligned with <c>TrackingAccessEvent</c> published by the QuickLink service.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class ClickTracking : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.click-tracking+json";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        public const string ID_KEY = "id";
        public const string FROM_KEY = "from";
        public const string TO_KEY = "to";
        public const string FROM_IDENTITY_KEY = "fromIdentity";
        public const string TO_IDENTITY_KEY = "toIdentity";
        public const string FROM_DOMAIN_KEY = "fromDomain";
        public const string TO_DOMAIN_KEY = "toDomain";
        public const string STORAGE_DATE_KEY = "storageDate";
        public const string CONTENT_MEDIA_TYPE_KEY = "contentMediaType";
        public const string CONTENT_HASH_KEY = "contentHash";
        public const string CONTENT_KEY = "content";
        public const string CONTENT_LENGTH_KEY = "contentLength";
        public const string SEQUENTIAL_ID_KEY = "sequentialId";
        public const string INTERNAL_ID_KEY = "internalId";
        public const string METADATA_KEY = "metadata";

        /// <summary>
        /// Initializes a new instance of the <see cref="ClickTracking"/> class.
        /// </summary>
        public ClickTracking()
            : base(MediaType)
        {
        }

        /// <summary>
        /// Gets or sets the event unique identifier (e.g. "quick-link:{uuid}").
        /// </summary>
        [DataMember(Name = ID_KEY)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the contact identity of the recipient.
        /// </summary>
        [DataMember(Name = FROM_KEY)]
        public string From { get; set; }

        /// <summary>
        /// Gets or sets the owner identity of the bot.
        /// </summary>
        [DataMember(Name = TO_KEY)]
        public string To { get; set; }

        /// <summary>
        /// Gets or sets the contact identity of the recipient (identity form).
        /// </summary>
        [DataMember(Name = FROM_IDENTITY_KEY)]
        public string FromIdentity { get; set; }

        /// <summary>
        /// Gets or sets the owner identity of the bot (identity form).
        /// </summary>
        [DataMember(Name = TO_IDENTITY_KEY)]
        public string ToIdentity { get; set; }

        /// <summary>
        /// Gets or sets the domain of the contact/user.
        /// </summary>
        [DataMember(Name = FROM_DOMAIN_KEY)]
        public string FromDomain { get; set; }

        /// <summary>
        /// Gets or sets the domain of the bot.
        /// </summary>
        [DataMember(Name = TO_DOMAIN_KEY)]
        public string ToDomain { get; set; }

        /// <summary>
        /// Gets or sets the access timestamp in ISO 8601 UTC format.
        /// </summary>
        [DataMember(Name = STORAGE_DATE_KEY)]
        public DateTime StorageDate { get; set; }

        /// <summary>
        /// Gets or sets the content media type from the tracking info.
        /// </summary>
        [DataMember(Name = CONTENT_MEDIA_TYPE_KEY)]
        public string ContentMediaType { get; set; }

        /// <summary>
        /// Gets or sets the content hash (MD5) of the serialized event content.
        /// </summary>
        [DataMember(Name = CONTENT_HASH_KEY)]
        public string ContentHash { get; set; }

        /// <summary>
        /// Gets or sets the Base64 encoded Unicode representation of the serialized event data.
        /// </summary>
        [DataMember(Name = CONTENT_KEY)]
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the byte length of the Base64 encoded content string.
        /// </summary>
        [DataMember(Name = CONTENT_LENGTH_KEY)]
        public long? ContentLength { get; set; }

        /// <summary>
        /// Gets or sets the sequential ID (not used, always null).
        /// </summary>
        [DataMember(Name = SEQUENTIAL_ID_KEY)]
        public long? SequentialId { get; set; }

        /// <summary>
        /// Gets or sets the internal ID (not used, always null).
        /// </summary>
        [DataMember(Name = INTERNAL_ID_KEY)]
        public string InternalId { get; set; }

        /// <summary>
        /// Gets or sets the event metadata serialized as a JSON string containing access details.
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
        public const string CORRELATION_ID_KEY = "correlationId";
        public const string IP_ADDRESS_KEY = "ipAddress";
        public const string USER_AGENT_KEY = "userAgent";
        public const string CLUSTER_KEY = "cluster";

        /// <summary>
        /// Gets or sets the correlation identifier linking this event to a broader flow.
        /// </summary>
        [DataMember(Name = CORRELATION_ID_KEY)]
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the client that performed the click.
        /// </summary>
        [DataMember(Name = IP_ADDRESS_KEY)]
        public string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the user agent string of the client that performed the click.
        /// </summary>
        [DataMember(Name = USER_AGENT_KEY)]
        public string UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the cluster identifier where the event originated.
        /// </summary>
        [DataMember(Name = CLUSTER_KEY)]
        public string Cluster { get; set; }
    }
}
