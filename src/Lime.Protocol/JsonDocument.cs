using System;
using System.Collections.Generic;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents a generic JSON document.
    /// </summary>
    public sealed class JsonDocument : DictionaryDocument<string, object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDocument"/> class.
        /// </summary>
        public JsonDocument()
            : this(new MediaType(MediaType.DiscreteTypes.Application, MediaType.SubTypes.JSON))
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDocument"/> class.
        /// </summary>
        public JsonDocument(MediaType mediaType)
            : this(new Dictionary<string, object>(), mediaType)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDocument"/> class.
        /// </summary>
        public JsonDocument(IDictionary<string, object> json, MediaType mediaType)
            : base(json, mediaType)
        {

        }

        public void SetMediaType(MediaType mediaType)
        {
            if (mediaType == null) throw new ArgumentNullException(nameof(mediaType));            
            if (!mediaType.IsJson) throw new ArgumentException("The media type is not a valid json type");            
            _mediaType = mediaType;
        }
    }
}
