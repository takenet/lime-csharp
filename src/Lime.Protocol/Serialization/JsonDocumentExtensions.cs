using Newtonsoft.Json;

namespace Lime.Protocol.Serialization
{
    public static class JsonDocumentExtensions
    {
        public static string ToJson(this JsonDocument document, JsonSerializerSettings settings) => JsonConvert.SerializeObject(document, settings);
    }
}
