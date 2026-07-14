using System.Text.Json;
using Newtonsoft.Json;

namespace Lime.Protocol.Serialization
{
    public static class JsonDocumentExtensions
    {
        public static string ToJson(this JsonDocument document, JsonSerializerSettings settings)
            => JsonConvert.SerializeObject(document, settings);

        /// <summary>
        /// Serializes a <see cref="JsonDocument"/> to a JSON string using System.Text.Json.
        /// </summary>
        public static string ToSystemTextJson(this JsonDocument document, JsonSerializerOptions options = null)
            => System.Text.Json.JsonSerializer.Serialize<System.Collections.Generic.IDictionary<string, object>>(
                document, options);
    }
}
