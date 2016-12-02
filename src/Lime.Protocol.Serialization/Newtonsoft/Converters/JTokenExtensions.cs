using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    public static class JTokenExtensions
    {
        public static object GetTokenValue(this JToken token)
        {
            var jValue = token as JValue;
            if (jValue != null)
            {
                return jValue.Value;
            }

            var jArray = token as JArray;
            if (jArray != null)
            {
                return jArray
                    .Select(GetTokenValue)
                    .ToArray();
            }

            var dictionary = token as IDictionary<string, JToken>;
            if (dictionary != null)
            {
                return dictionary.ToDictionary(k => k.Key, v => GetTokenValue(v.Value));
            }

            throw new ArgumentException("Unknown token type");
        }

        public static Document ToDocument(this JToken jToken, MediaType mediaType, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            Type documentType;
            Document document;
            if (TypeUtil.TryGetTypeForMediaType(mediaType, out documentType))
            {
                if (mediaType.IsJson)
                {
                    document = (Document)serializer.Deserialize(jToken.CreateReader(), documentType);                    
                }
                else if (jToken != null)
                {
                    var parseFunc = TypeUtilEx.GetParseFuncForType(documentType);
                    document = (Document)parseFunc(jToken.ToString());
                }
                else
                {
                    document = (Document)Activator.CreateInstance(documentType);
                }
            }
            else
            {
                if (mediaType.IsJson)
                {
                    var contentJsonObject = jToken as IDictionary<string, JToken>;
                    if (contentJsonObject != null)
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
            }
            return document;
        }
    }
}