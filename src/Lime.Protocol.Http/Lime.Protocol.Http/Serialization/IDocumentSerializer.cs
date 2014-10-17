using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lime.Protocol.Http.Serialization
{
    public interface IDocumentSerializer
    {
        string Serialize(Document document);

        Document Deserialize(string documentString, MediaType mediaType);        
    }
}
