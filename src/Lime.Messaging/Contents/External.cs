using Lime.Protocol;
using System.Runtime.Serialization;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents an external message.
    /// </summary>
    [DataContract]
    public class External : Document
    {
        public static readonly string MimeType = "application/vnd.lime.external+json";
        public static readonly MediaType MediaType = MediaType.Parse(MimeType);

        public const string TYPE_KEY = "type";
        public const string CONTENT_KEY = "content";
        /// <summary>
        /// Initializes a new instance of the <see cref="Contents.External"/> class.
        /// </summary>
        public External()
            : base(MediaType)
        {
        }

        /// <summary>
        /// Gets or sets the type of external message content type.
        /// </summary>
        [DataMember(Name = TYPE_KEY)]
        public MediaType Type => Content?.GetMediaType();

        /// <summary>
        /// Gets or sets the type of external message content.
        /// </summary>
        [DataMember(Name = CONTENT_KEY)]
        public Document Content { get; set; }
    }
}