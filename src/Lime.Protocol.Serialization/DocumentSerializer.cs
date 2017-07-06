using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Lime.Protocol.Serialization.Newtonsoft;

namespace Lime.Protocol.Serialization
{
    public sealed class DocumentSerializer : IDocumentSerializer
    {
        public static readonly JsonSerializer JsonSerializer = JsonSerializer.Create(JsonNetSerializer.Settings);

        public Document Deserialize(string value, MediaType mediaType)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (mediaType == null) throw new ArgumentNullException(nameof(mediaType));

            Type documentType;
            if (mediaType.IsJson)
            {
                var jsonObject = JObject.Parse(value);
                if (TypeUtil.TryGetTypeForMediaType(mediaType, out documentType))
                {
                    return (Document)jsonObject.ToObject(documentType, JsonSerializer);
                }
                var dictionary = jsonObject.ToObject<Dictionary<string, object>>(JsonSerializer);
                return new JsonDocument(dictionary, mediaType);
            }

            if (TypeUtil.TryGetTypeForMediaType(mediaType, out documentType))
            {
                var parseFunc = TypeUtilEx.GetParseFuncForType(documentType);
                return parseFunc(value) as Document;
            }
            return new PlainDocument(value, mediaType);
        }

        public string Serialize(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            if (!document.GetMediaType().IsJson) return document.ToString();

            var jsonObject = JObject.FromObject(document, JsonSerializer);
            return JsonConvert.SerializeObject(jsonObject, Formatting.None, JsonNetSerializer.Settings);
        }
    }
}
