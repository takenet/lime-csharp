using Lime.Protocol;
using System.Runtime.Serialization;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Defines a list of <see cref="Document"/> with a header.
    /// </summary>
    /// <seealso cref="Document" />
    [DataContract]
    public class DocumentList : Document
    {            
        public const string MIME_TYPE = "application/vnd.lime.list+json";
        public const string HEADER_KEY = "header";
        public const string ITEMS_KEY = "items";

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentList"/> class.
        /// </summary>
        public DocumentList()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }

        /// <summary>
        /// Gets or sets the list header document.
        /// </summary>
        /// <value>
        /// The header.
        /// </value>
        [DataMember(Name = HEADER_KEY)]
        public DocumentContainer Header { get; set; }
 
        /// <summary>
        /// Gets or sets the list items.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        [DataMember(Name = ITEMS_KEY)]
        public DocumentContainer[] Items { get; set; }
    }
}
