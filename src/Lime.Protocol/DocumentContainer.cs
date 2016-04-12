using System.Runtime.Serialization;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents a generic container for a document, providing a media type for the correct handling of its value by the nodes.
    /// This class can be used along with <see cref="DocumentCollection"/> to traffic different document types in a single message.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Document" />
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class DocumentContainer : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.container+json";        
        public const string TYPE_KEY = "type";
        public const string VALUE_KEY = "value";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentContainer"/> class.
        /// </summary>
        public DocumentContainer() : 
            base(MediaType)
        {
        }

        /// <summary>
        /// Gets the media type of the contained document.
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
