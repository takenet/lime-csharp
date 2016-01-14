using System.Collections;
using System.Runtime.Serialization;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents a collection
    /// of documents.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class DocumentCollection : Document, IEnumerable
    {
        public const string MIME_TYPE = "application/vnd.lime.collection+json";

        public const string TOTAL_KEY = "total";
        public const string ITEM_TYPE_KEY = "itemType";
        public const string ITEMS_KEY = "items";
        
        #region Constructor

        public DocumentCollection()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }

        #endregion

        #region Public Properties

        [DataMember(Name = TOTAL_KEY, EmitDefaultValue = false)]
        public int Total { get; set; }

        [DataMember(Name = ITEM_TYPE_KEY)]
        public MediaType ItemType { get; set; }

        [DataMember(Name = ITEMS_KEY)]
        public Document[] Items { get; set; }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            if (Items != null)
            {
                return Items.GetEnumerator();
            }

            return null;
        }

        #endregion
    }
}
