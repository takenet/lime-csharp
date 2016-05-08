using System;
using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents an external link to a media content.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Document" />
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class MediaLink : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.media-link+json";       
        public const string TYPE_KEY = "type";
        public const string URI_KEY = "uri";
        public const string SIZE_KEY = "size";
        public const string PREVIEW_URI_KEY = "previewUri";
        public const string PREVIEW_TYPE_KEY = "previewType";
        public const string TEXT_KEY = "text";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaLink"/> class.
        /// </summary>
        public MediaLink() 
            : base(MediaType)
        {
        }

        /// <summary>
        /// Gets or sets the media type of the linked media.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [DataMember(Name = TYPE_KEY)]
        public MediaType Type { get; set; }

        /// <summary>
        /// Gets or sets the media URI.
        /// </summary>
        /// <value>
        /// The URI.
        /// </value>
        [DataMember(Name = URI_KEY)]
        public Uri Uri { get; set; }

        /// <summary>
        /// Gets or sets the media size, in bytes.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        [DataMember(Name = SIZE_KEY)]
        public long? Size { get; set; }

        /// <summary>
        /// Gets or sets the media preview URI. 
        /// It can be used to provide a smaller size representation of the media, like a thumbnail for a video or image.
        /// </summary>
        /// <value>
        /// The thumbnail URI.
        /// </value>
        [DataMember(Name = PREVIEW_URI_KEY)]
        public Uri PreviewUri { get; set; }

        /// <summary>
        /// Gets or sets the type of the media preview.
        /// </summary>
        /// <value>
        /// The type of the thumbnail.
        /// </value>
        [DataMember(Name = PREVIEW_TYPE_KEY)]
        public MediaType PreviewType { get; set; }

        /// <summary>
        /// Gets or sets the media description text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        [DataMember(Name = TEXT_KEY)]
        public string Text { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{Text} {Uri}";
    }
}
