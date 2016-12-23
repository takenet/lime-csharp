using System;
using System.Collections.Generic;
using System.Text;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents a plain document.
    /// </summary>
    public sealed class PlainDocument : Document
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlainDocument"/> class.
        /// </summary>
        /// <param name="mediaType">Type of the media.</param>
        public PlainDocument(MediaType mediaType)
            : this(null, mediaType)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlainDocument"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="mediaType">Type of the media.</param>
        /// <exception cref="System.ArgumentException">Invalid media type. The suffix value should be empty.</exception>
        public PlainDocument(string value, MediaType mediaType)
            : base(mediaType)
        {
            if (!string.IsNullOrWhiteSpace(mediaType.Suffix))
            {
                throw new ArgumentException("Invalid media type. The suffix value should be empty.", nameof(mediaType));
            }

            Value = value;
        }

        /// <summary>
        /// The value of the document.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => Value;

        /// <summary>
        /// Performs an implicit conversion from <see cref="Lime.Protocol.PlainDocument" /> to <see cref="System.String" />.
        /// </summary>
        /// <param name="plainDocument">The plain document.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator string(PlainDocument plainDocument) => plainDocument.Value;
    }
}
