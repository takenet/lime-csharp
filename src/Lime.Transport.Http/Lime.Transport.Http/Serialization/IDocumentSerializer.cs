using Lime.Protocol;

namespace Lime.Transport.Http.Protocol.Serialization
{
    public interface IDocumentSerializer
    {
        string Serialize(Document document);

        Document Deserialize(string documentString, MediaType mediaType);        
    }
}
