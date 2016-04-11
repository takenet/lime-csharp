using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    /// <summary>
    /// Defines a entity with a <see cref="MediaType"/>.
    /// </summary>
    public interface IDocument
    {
        /// <summary>
        /// Gets the type of the media for the document.
        /// </summary>
        /// <returns></returns>
        MediaType GetMediaType();
    }
}
