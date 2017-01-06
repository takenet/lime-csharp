using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lime.Protocol.Serialization
{
    public sealed class DocumentSerializer : IDocumentSerializer
    {
        public string Serialize(Document document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var mediaType = document.GetMediaType();
            if (mediaType.IsJson)
            {
                return JsonConvert.SerializeObject(document);
            }
            return document.ToString();
        }

        public Document Deserialize(string documentString, MediaType mediaType)
        {
            if (documentString == null)
            {
                throw new ArgumentNullException(nameof(documentString));
            }

            if (mediaType == null)
            {
                throw new ArgumentNullException(nameof(mediaType));
            }

            Document document = null;
            Type type;
            if (TypeUtil.TryGetTypeForMediaType(mediaType, out type))
            {
                if (mediaType.IsJson)
                {
                    document = JsonConvert.DeserializeObject(documentString, type) as Document;
                }
                else
                {
                    var parseFunc = TypeUtilEx.GetParseFuncForType(type);
                    document = parseFunc(documentString) as Document;
                }                
            }
            else
            {
                if (mediaType.IsJson)
                {
                    var json = JObject.Parse(documentString);
                    document = new JsonDocument(json.ToObject<Dictionary<string, object>>(), mediaType);
                }
                else
                {
                    document = new PlainDocument(documentString, mediaType);
                }
            }

            return document;           
        }
    }
}
