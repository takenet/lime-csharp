using System;
using System.Runtime.Serialization;

namespace Lime.Protocol
{
    /// <summary>
    /// Defines a entity with a <see cref="MediaType"/>.
    /// </summary>
    /// <seealso cref="Lime.Protocol.IDocument" />
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public abstract class Document : IDocument
    {
        protected MediaType _mediaType;

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class.
        /// </summary>
        /// <param name="mediaType">Type of the media.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        protected Document(MediaType mediaType)
        {
            if (mediaType == null) throw new ArgumentNullException(nameof(mediaType));            
            _mediaType = mediaType;
        }

        /// <summary>
        /// Gets the type of the media for the document.
        /// </summary>
        /// <returns></returns>
        public MediaType GetMediaType()
        {
            return _mediaType;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="Document"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Document(string value)
        {
            if (value == null) return null;
            return new PlainDocument(value, MediaType.TextPlain);
        }
    }
}
