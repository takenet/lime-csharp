using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents a container for sensitive documents from the user that should not be stored.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Document" />
    public class SensitiveContainer : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.sensitive+json";
        public const string TYPE_KEY = "type";
        public const string VALUE_KEY = "value";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="SensitiveContainer"/> class.
        /// </summary>
        public SensitiveContainer()
            : base(MediaType)
        {

        }

        /// <summary>
        /// Gets the media type of the sensitive document.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [DataMember(Name = TYPE_KEY)]
        public MediaType Type => Value?.GetMediaType();

        /// <summary>
        /// Gets or sets the contained document value.
        /// </summary>
        /// <value>
        /// The document.
        /// </value>
        [DataMember(Name = VALUE_KEY)]
        public Document Value { get; set; }
    }
}
