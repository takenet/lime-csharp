using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    public static class JTokenExtensions
    {
        public static object GetTokenValue(this JToken token)
        {
            if (token is JValue jValue)
            {
                return jValue.Value;
            }

            if (token is JArray jArray)
            {
                return jArray
                    .Select(GetTokenValue)
                    .ToArray();
            }

            if (token is IDictionary<string, JToken> dictionary)
            {
                return dictionary.ToDictionary(k => k.Key, v => GetTokenValue(v.Value));
            }

            throw new ArgumentException("Unknown token type");
        }

        public static Document ToDocument(this JToken jToken, MediaType mediaType, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            Document document;
            if (TypeUtil.TryGetTypeForMediaType(mediaType, out var documentType))
            {
                try
                {
                    if (mediaType.IsJson)
                    {
                        document = (Document) serializer.Deserialize(jToken.CreateReader(), documentType);
                    }
                    else if (jToken != null)
                    {
                        var parseFunc = TypeUtilEx.GetParseFuncForType(documentType);
                        document = (Document) parseFunc(jToken.ToString());
                    }
                    else
                    {
                        document = (Document) Activator.CreateInstance(documentType);
                    }

                    return document;
                }
                catch (JsonSerializationException) { }
                catch (ArgumentException) { }
                catch (TypeLoadException)
                {
                    // Ignore deserialization exceptions and return a Plain/Json document instead
                }
            }

            if (mediaType.IsJson)
            {
                if (jToken is IDictionary<string, JToken> contentJsonObject)
                {
                    var contentDictionary = contentJsonObject.ToDictionary(k => k.Key, v => v.Value.GetTokenValue());
                    document = new JsonDocument(contentDictionary, mediaType);
                }
                else
                {
                    throw new ArgumentException("The property is not a JSON");
                }
            }
            else
            {
                document = new PlainDocument(jToken.ToString(), mediaType);
            }
            return document;
        }
    }
}