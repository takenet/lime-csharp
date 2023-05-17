using System.Runtime.Serialization;
using Lime.Protocol;

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

        public const string CONTENT = "content";
        public const string REPLY_TO = "replyTo";
        /// <summary>
        /// Initializes a new instance of the <see cref="Reply"/> class.
        /// </summary>
        public Reply()
            : base(MediaType)
        {
        }

        /// <summary>
        /// Gets or sets the content of the reply.
        /// </summary>
        [DataMember(Name = CONTENT)]
        public DocumentContainer Content { get; set; }

        /// <summary>
        /// Gets or sets the reference to the message being replied to.
        /// </summary>
        [DataMember(Name = REPLY_TO)]
        public ReplyTo ReplyTo { get; set; }
    }

    /// <summary>
    /// Represents the document container for the reply reference.
    /// </summary>
    [DataContract]
    public class ReplyTo
    {
        public const string ID = "id";
        public const string CONTENT = "content";

        /// <summary>
        /// Gets or sets the identifier of the message being replied to.
        /// </summary>
        [DataMember(Name = ID)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the content of the reply.
        /// </summary>
        [DataMember(Name = CONTENT)]
        public DocumentContainer Content { get; set; }
    }
}