using System.Runtime.Serialization;
using Lime.Protocol;
using static System.Net.Mime.MediaTypeNames;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents a reply message.
    /// </summary>
    [DataContract]
    public class Reply : Document
    {
        public static readonly string MimeType = "application/vnd.lime.reply+json";
        public static readonly MediaType MediaType = MediaType.Parse(MimeType);

        public const string REPLIED = "replied";
        public const string IN_REPLY_TO = "inReplyTo";
        /// <summary>
        /// Initializes a new instance of the <see cref="Contents.Reply"/> class.
        /// </summary>
        public Reply()
            : base(MediaType)
        {
        }

        /// <summary>
        /// Gets or sets the content of the reply.
        /// </summary>
        [DataMember(Name = REPLIED)]
        public DocumentContainer Replied { get; set; }

        /// <summary>
        /// Gets or sets the reference to the message being replied to.
        /// </summary>
        [DataMember(Name = IN_REPLY_TO)]
        public InReplyTo InReplyTo { get; set; }
    }

    /// <summary>
    /// Represents the document container for the reply reference.
    /// </summary>
    [DataContract]
    public class InReplyTo
    {
        public const string ID = "id";
        public const string TYPE_KEY = "type";
        public const string VALUE_KEY = "value";

        /// <summary>
        /// Gets or sets the identifier of the message being replied to.
        /// </summary>
        /// <value>
        /// The string.
        /// </value>   
        [DataMember(Name = ID)]
        public string Id { get; set; }

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