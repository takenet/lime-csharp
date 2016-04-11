using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Aggregates a list of <see cref="Option"/> for selection.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Document" />
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Select : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.select+json";
        public const string TEXT_KEY = "text";
        public const string DESTINATION_KEY = "destination";
        public const string OPTIONS_KEY = "options";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="Select"/> class.
        /// </summary>
        public Select() 
            : base(MediaType)
        {

        }

        /// <summary>
        /// Gets or sets the select question text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        [DataMember(Name = TEXT_KEY)]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the destination which the selected option should be sent to.
        /// If not defined, the selected option should be sent to the caller.
        /// </summary>
        /// <value>
        /// The destination.
        /// </value>
        [DataMember(Name = DESTINATION_KEY)]
        public Node Destination { get; set; }

        /// <summary>
        /// Gets or sets the available select options.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        [DataMember(Name = OPTIONS_KEY)]
        public Option[] Options { get; set; }

        [DataMember(Name = "defaultOption")]
        public Option DefaultOption { get; set; }

    }

    /// <summary>
    /// Defines a option to be selected by the destination.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Option
    {
        public const string TEXT_KEY = "text";
        public const string TYPE_KEY = "type";
        public const string VALUE_KEY = "value";
        
        /// <summary>
        /// Gets or sets the option label text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        [DataMember(Name = TEXT_KEY)]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets media type of the option <see cref="Value"/>.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>        
        [DataMember(Name = TYPE_KEY)]
        public MediaType Type => Value?.GetMediaType();

        /// <summary>
        /// Gets or sets the option value to be returned to the caller.
        /// If not defined, the value of <see cref="Text"/> should be returned.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        [DataMember(Name = VALUE_KEY)]
        public Document Value { get; set; }     
    }
}
