using Lime.Protocol;
using System.Runtime.Serialization;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents a copy and paste document.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class CopyAndPaste : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.copy-and-paste+json";
        public const string HEADER_KEY = "header";
        public const string FOOTER_KEY = "footer";
        public const string BUTTON_KEY = "button";
        public const string BODY_KEY = "body";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="CopyAndPaste"/> class.
        /// </summary>
        public CopyAndPaste()
            : base(MediaType)
        {
        }

        /// <summary>
        /// Gets or sets the title of the copy and paste document.
        /// </summary>
        [DataMember(Name = HEADER_KEY)]
        public string Header { get; set; }
        /// <summary>
        /// Gets or sets the body of the copy and paste document.
        /// </summary>
        [DataMember(Name = BODY_KEY)]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the footer of the copy and paste document.
        /// </summary>
        [DataMember(Name = FOOTER_KEY)]
        public string Footer { get; set; }

        /// <summary>
        /// Gets or sets the button of the copy and paste document.
        /// </summary>
        [DataMember(Name = BUTTON_KEY)]
        public Button Button { get; set; }
    }

    /// <summary>
    /// Represents a button in a copy and paste document.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Button
    {
        public const string TEXT_KEY = "text";
        public const string VALUE_KEY = "value";

        /// <summary>
        /// Gets or sets the text of the button.
        /// </summary>
        [DataMember(Name = TEXT_KEY)]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the value of the button.
        /// </summary>
        [DataMember(Name = VALUE_KEY)]
        public string Value { get; set; }
    }
}
