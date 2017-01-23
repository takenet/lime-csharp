using Lime.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Aggregates multiple documents of different types.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Document" />
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class DocumentTuple : Document, IEnumerable
    {
        public const string MIME_TYPE = "application/vnd.lime.tuple+json";
        public const string ITEM1_KEY = "item1";
        public const string ITEM2_KEY = "item2";
        public const string ITEM3_KEY = "item3";
        public const string ITEM4_KEY = "item4";
        public const string ITEM5_KEY = "item5";

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentTuple"/> class.
        /// </summary>
        public DocumentTuple()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }

        /// <summary>
        /// Gets or sets the item 1.
        /// </summary>
        /// <value>
        /// The item 1.
        /// </value>
        [DataMember(Name = ITEM1_KEY)]
        public DocumentContainer Item1 { get; set; }

        /// <summary>
        /// Gets or sets the item 2.
        /// </summary>
        /// <value>
        /// The item 2.
        /// </value>
        [DataMember(Name = ITEM2_KEY)]
        public DocumentContainer Item2 { get; set; }

        /// <summary>
        /// Gets or sets the item 3.
        /// </summary>
        /// <value>
        /// The item 3.
        /// </value>
        [DataMember(Name = ITEM3_KEY)]
        public DocumentContainer Item3 { get; set; }

        /// <summary>
        /// Gets or sets the item 4.
        /// </summary>
        /// <value>
        /// The item 4.
        /// </value>
        [DataMember(Name = ITEM4_KEY)]
        public DocumentContainer Item4 { get; set; }

        /// <summary>
        /// Gets or sets the item 5.
        /// </summary>
        /// <value>
        /// The item 5.
        /// </value>
        [DataMember(Name = ITEM5_KEY)]
        public DocumentContainer Item5 { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Document" /> with the specified index. The index value is 0 based.
        /// </summary>
        /// <value>
        /// The <see cref="Document" />.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// </exception>
        public Document this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Item1?.Value;
                    case 1: return Item2?.Value;
                    case 2: return Item3?.Value;
                    case 3: return Item4?.Value;
                    case 4: return Item5?.Value;
                    default:
                        throw new ArgumentOutOfRangeException();

                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        Item1 = new DocumentContainer() { Value = value };
                        break;
                    case 1:
                        Item2 = new DocumentContainer() { Value = value };
                        break;
                    case 2:
                        Item3 = new DocumentContainer() { Value = value };
                        break;
                    case 3:
                        Item4 = new DocumentContainer() { Value = value };
                        break;
                    case 4:
                        Item5 = new DocumentContainer() { Value = value };
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

        }

        public IEnumerable<Document> GetDocuments()
        {
            if (Item1 != null) yield return Item1.Value;
            if (Item2 != null) yield return Item2.Value;
            if (Item3 != null) yield return Item3.Value;
            if (Item4 != null) yield return Item4.Value;
            if (Item5 != null) yield return Item5.Value;
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator() => GetDocuments().GetEnumerator();
    }
}
