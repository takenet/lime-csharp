using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    /// <summary>
    /// Represents a document with a Media Type
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public abstract partial class Document : IDocument
    {
        private MediaType _mediaType;

        #region Constructor

        public Document(MediaType mediaType)
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
