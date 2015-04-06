using System;
using System.Runtime.Serialization;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents a document with a Media Type
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public abstract class Document : IDocument
    {
        protected MediaType _mediaType;

        #region Constructor

        protected Document(MediaType mediaType)
        {
            if (mediaType == null)
            {
                throw new ArgumentNullException("mediaType");
            }

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
