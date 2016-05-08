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

        #region Constructor

        protected Document(MediaType mediaType)
        {
            if (mediaType == null) throw new ArgumentNullException(nameof(mediaType));            
            _mediaType = mediaType;
        }

        #endregion

        #region IDocument Members

        public MediaType GetMediaType()
        {
            return _mediaType;
        }

        #endregion
    }
}
