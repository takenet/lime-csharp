using System;
using Lime.Protocol;
using Lime.Protocol.Serialization;

namespace Lime.Transport.Http.Protocol.Serialization
{
    public sealed class DocumentSerializer : IDocumentSerializer
    {

        #region IDocumentSerializer Members

        public string Serialize(Document document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            var mediaType = document.GetMediaType();
            if (mediaType.IsJson)
            {
                return JsonSerializer.Serialize(document);
            }
            else
            {
                return document.ToString();
            }            
        }

        public Document Deserialize(string documentString, MediaType mediaType)
        {
            if (documentString == null)
            {
                throw new ArgumentNullException("documentString");
            }

            if (mediaType == null)
            {
                throw new ArgumentNullException("mediaType");
            }

            Document document = null;
            Type type;
            if (TypeUtil.TryGetTypeForMediaType(mediaType, out type))
            {
                if (mediaType.IsJson)
                {
                    document = JsonSerializer.Deserialize(type, documentString) as Document;
                }
                else
                {
                    var parseFunc = TypeUtil.GetParseFuncForType(type);
                    document = parseFunc(documentString) as Document;
                }                
            }
            else
            {
                if (mediaType.IsJson)
                {
                    var json = JsonObject.ParseJson(documentString);
                    document = new JsonDocument(json, mediaType);
                }
                else
                {
                    document = new PlainDocument(documentString, mediaType);
                }
            }

            return document;           
        }

        #endregion
    }
}
