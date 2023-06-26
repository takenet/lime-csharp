using System;
using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Defines a base class for link documents.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Document" />
    public abstract class Link : Document
    {
        public const string URI_KEY = "uri";
        public const string PREVIEW_URI_KEY = "previewUri";
        public const string PREVIEW_TYPE_KEY = "previewType";
        public const string TITLE_KEY = "title";
        public const string TEXT_KEY = "text";
        public const string AUTHORIZATION_REQUIRED_KEY = "authorizationRequired";

        /// <summary>
        /// Initializes a new instance of the <see cref="Link"/> class.
        /// </summary>
        /// <param name="mediaType">Type of the media.</param>
        protected Link(MediaType mediaType) 
            : base(mediaType)
        {
        }

        /// <summary>
        /// Gets or sets the link URI.
        /// </summary>
        /// <value>
        /// The URI.
        /// </value>
        [DataMember(Name = URI_KEY)]
        public Uri Uri { get; set; }

        /// <summary>
        /// Gets or sets the link preview URI. 
        /// It can be used to provide a preview of a website page or media, like a thumbnail image.
        /// </summary>
        /// <value>
        /// The thumbnail URI.
        /// </value>
        [DataMember(Name = PREVIEW_URI_KEY)]
        public Uri PreviewUri { get; set; }

        /// <summary>
        /// Gets or sets the type of the link preview.
        /// </summary>
        /// <value>
        /// The type of the thumbnail.
        /// </value>
        [DataMember(Name = PREVIEW_TYPE_KEY)]
        public MediaType PreviewType { get; set; }

        /// <summary>
        /// Gets or sets the link title text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        [DataMember(Name = TITLE_KEY)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the link description text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        [DataMember(Name = TEXT_KEY)]
        public string Text { get; set; }

        /// <summary>
        /// Defines whether authorization is required for the link.
        /// </summary>
        [DataMember(Name = AUTHORIZATION_REQUIRED_KEY)]
        public bool? AuthorizationRequired { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{Text} {Uri}";
    }
}