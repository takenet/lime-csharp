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
        #region Constructor

        public PlainDocument(MediaType mediaType)
            : this(null, mediaType)
        {

        }

        public PlainDocument(string value, MediaType mediaType)
            : base(mediaType)
        {
            if (!string.IsNullOrWhiteSpace(mediaType.Suffix))
            {
                throw new ArgumentException("Invalid media type. The suffix value should be empty.");
            }

            this.Value = value;
        }

        #endregion

        /// <summary>
        /// The value of the document
        /// </summary>
        public string Value { get; private set; }

        public override string ToString()
        {
            return this.Value;
        }
    }
}
