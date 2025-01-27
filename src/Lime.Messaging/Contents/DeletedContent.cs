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

        /// <summary>
        /// Empty message text representing deleted content
        /// </summary>
        public string Text => string.Empty;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance of the <see cref="Contents.DeletedContent"/> class.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => Text;
    }
}
