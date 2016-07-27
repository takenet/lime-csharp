using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Aggregates a list of <see cref="DocumentSelectOption"/> for selection.
    /// This class is similar to the <see cref="Select"/>, but allows generic documents to be defined in the select header and options, instead of plain text.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Document" />
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class DocumentSelect : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.document-select+json";
        public const string SCOPE_KEY = "scope";
        public const string HEADER_KEY = "header";
        public const string OPTIONS_KEY = "options";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentSelect"/> class.
        /// </summary>
        public DocumentSelect() 
            : base(MediaType)
        {

        }

        /// <summary>
        /// Gets or sets the scope which the select options is valid.
        /// This property hints to the destination of the select when the sender is able to receive and understand a select option reply.
        /// </summary>
        /// <value>
        /// The scope.
        /// </value>
        [DataMember(Name = SCOPE_KEY)]
        public SelectScope? Scope { get; set; }

        /// <summary>
        /// Gets or sets the select header document.
        /// </summary>
        /// <value>
        /// The header.
        /// </value>
        [DataMember(Name = HEADER_KEY)]        
        public DocumentContainer Header { get; set; }

        /// <summary>
        /// Gets or sets the available select options.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        [DataMember(Name = OPTIONS_KEY)]
        public DocumentSelectOption[] Options { get; set; }
    }

    /// <summary>
    /// Defines a option to be selected by the destination.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class DocumentSelectOption
    {
        public const string ORDER_KEY = "order";
        public const string LABEL_KEY = "label";
        public const string VALUE_KEY = "value";

        /// <summary>
        /// Gets or sets the option order number.
        /// </summary>
        /// <value>
        /// The order.
        /// </value>
        [DataMember(Name = ORDER_KEY)]
        public int? Order { get; set; }

        /// <summary>
        /// Gets or sets the option label document.
        /// </summary>
        /// <value>
        /// The label.
        /// </value>
        [DataMember(Name = LABEL_KEY)]
        public DocumentContainer Label { get; set; }

        /// <summary>
        /// Gets or sets the option value to be returned to the caller.
        /// If not defined, no value should be returned.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        [DataMember(Name = VALUE_KEY)]
        public DocumentContainer Value { get; set; }
    }
}
