namespace Lime.Protocol.Serialization
{
    public interface IDocumentSerializer
    {
        string Serialize(Document document);

        Document Deserialize(string value, MediaType mediaType);
    }
}
