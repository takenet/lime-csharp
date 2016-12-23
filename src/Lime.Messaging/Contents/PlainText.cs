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
    /// Represents a flat text content
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class PlainText : Document
    {
        public const string MIME_TYPE = "text/plain";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="PlainText"/> class.
        /// </summary>
        public PlainText()
            : base(MediaType)
        {

        }

        /// <summary>
        /// Text of the message
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => Text;

        /// <summary>
        /// Parses the string to a 
        /// PlainText instance.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static PlainText Parse(string value) => new PlainText() { Text = value };

        /// <summary>
        /// Performs an implicit conversion from <see cref="PlainText"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator string(PlainText plainText) => plainText.Text;
    }
}