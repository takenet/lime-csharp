using Lime.Protocol;

namespace Lime.Transport.Http.Serialization
{
    public interface IDocumentSerializer
    {
        string Serialize(Document document);

        Document Deserialize(string documentString, MediaType mediaType);        
    }
}
