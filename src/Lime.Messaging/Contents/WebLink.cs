using System;
using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents an external link to a website page.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Document" />
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class WebLink : Link
    {
        public const string MIME_TYPE = "application/vnd.lime.web-link+json";
        public const string TARGET_KEY = "target";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebLink"/> class.
        /// </summary>
        public WebLink() 
            : base(MediaType)
        {

        }

        /// <summary>
        /// Gets or sets the target for showing the web link content.
        /// </summary>
        /// <value>
        /// The target.
        /// </value>
        [DataMember(Name = TARGET_KEY)]
        public WebLinkTarget? Target { get; set; }
    }

    /// <summary>
    /// Defines the available web link targets.
    /// </summary>
    [DataContract]
    public enum WebLinkTarget
    {
        /// <summary>
        /// Indicates that the web link content should be displayed in a new container.
        /// </summary>
        [EnumMember(Value = "blank")]
        Blank,
        /// <summary>
        /// Indicates that the web link content should be displayed in the current container.
        /// </summary>
        [EnumMember(Value = "self")]
        Self,
        /// <summary>
        /// Indicates that the web link content should be displayed compacted in the current container.
        /// </summary>
        [EnumMember(Value = "selfCompact")]
        SelfCompact,
        /// <summary>
        /// Indicates that the web link content should be displayed tall in the current container.
        /// </summary>
        [EnumMember(Value = "selfTall")]
        SelfTall,
    }
}
