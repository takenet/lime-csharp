using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Contents
{
    /// <summary>
    /// Represents a flat text content
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public partial class TextContent : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.text+json";
        public const string TEXT_KEY = "text";

        public TextContent()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }

        /// <summary>
        /// Text of the message
        /// </summary>
        [DataMember(Name = TEXT_KEY)]
        public string Text { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Text;
        }
    }
}