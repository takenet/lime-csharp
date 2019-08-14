using Lime.Protocol;
using System.Runtime.Serialization;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents a text and its version to be spoken by a speech software.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Document" />
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class SsmlText : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.ssml-text+json";
        public const string TEXT_KEY = "text";
        public const string SSML_KEY = "ssml";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="SsmlText"/> class.
        /// </summary>
        public SsmlText()
            : base(MediaType)
        {

        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        [DataMember(Name = TEXT_KEY)]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the ssml (Speech Synthesis Markup Language).
        /// </summary>
        /// <value>
        /// The ssml (Speech Synthesis Markup Language).
        /// </value>
        [DataMember(Name = SSML_KEY)]
        public string Ssml { get; set; }
    }
}
