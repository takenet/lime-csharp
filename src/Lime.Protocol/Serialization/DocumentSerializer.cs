using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Lime.Protocol.Serialization.Newtonsoft;

namespace Lime.Protocol.Serialization
{
    public sealed class DocumentSerializer : IDocumentSerializer
    {
        private readonly IDocumentTypeResolver _documentTypeResolver;
        private readonly JsonSerializer _jsonSerializer;
        private readonly JsonSerializerSettings _settings;

        public DocumentSerializer(IDocumentTypeResolver documentTypeResolver)
        {
            _documentTypeResolver = documentTypeResolver;
            _settings = EnvelopeSerializer.CreateSettings(documentTypeResolver);
            _jsonSerializer = JsonSerializer.Create(_settings);
        }

        public Document Deserialize(string value, MediaType mediaType)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (mediaType == null) throw new ArgumentNullException(nameof(mediaType));

            Type documentType;
            if (mediaType.IsJson)
            {
                var jsonObject = JObject.Parse(value);
                if (_documentTypeResolver.TryGetTypeForMediaType(mediaType, out documentType))
                {
                    return (Document)jsonObject.ToObject(documentType, _jsonSerializer);
                }
                var dictionary = jsonObject.ToObject<Dictionary<string, object>>(_jsonSerializer);
                return new JsonDocument(dictionary, mediaType);
            }

            if (_documentTypeResolver.TryGetTypeForMediaType(mediaType, out documentType))
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

            var jsonObject = JObject.FromObject(document, _jsonSerializer);
            return JsonConvert.SerializeObject(jsonObject, Formatting.None, _settings);
        }
    }
}
