using System.Collections;
using System.Runtime.Serialization;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents a collection of documents.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class DocumentCollection : Document, IEnumerable
    {
        public const string MIME_TYPE = "application/vnd.lime.collection+json";
        public const string TOTAL_KEY = "total";
        public const string ITEM_TYPE_KEY = "itemType";
        public const string ITEMS_KEY = "items";

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentCollection"/> class.
        /// </summary>
        public DocumentCollection()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }

        /// <summary>
        /// Gets or sets the total of items in the collection.
        /// The count refers to the original source collection, without any applied filter that may be applied in the items on this collection.
        /// </summary>
        /// <value>
        /// The total.
        /// </value>
        [DataMember(Name = TOTAL_KEY, EmitDefaultValue = false)]
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets the media type of all items of the collection..
        /// </summary>
        /// <value>
        /// The type of the item.
        /// </value>
        [DataMember(Name = ITEM_TYPE_KEY)]
        public MediaType ItemType { get; set; }

        /// <summary>
        /// Gets or sets the collection items.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        [DataMember(Name = ITEMS_KEY)]
        public Document[] Items { get; set; }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator() => Items?.GetEnumerator();
    }
}
