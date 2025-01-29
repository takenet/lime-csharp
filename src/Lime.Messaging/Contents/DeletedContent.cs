using Lime.Protocol;
using System.Runtime.Serialization;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents a message that has the content deleted
    /// </summary>
    [DataContract]
    public class DeletedContent : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.deleted-content+json";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="Contents.DeletedContent"/> class.
        /// </summary>
        public DeletedContent()
        : base(MediaType)
        {
        }
    }
}
