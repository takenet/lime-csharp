using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    public class DocumentContainerJsonConverter : JsonConverter
    {
        public const string TYPE_KEY = "type";

        public override bool CanWrite => false;

        public override bool CanRead => true;

        public override bool CanConvert(Type objectType)
        {
            if (objectType.IsAbstract) return false;

            var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return
                properties.Any(p => p.PropertyType == typeof(Document)) &&
                properties.Any(p => p.Name.Equals(TYPE_KEY, StringComparison.OrdinalIgnoreCase) && p.PropertyType == typeof(MediaType));
        }

        public override void WriteJson(JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotSupportedException();         
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            global::Newtonsoft.Json.JsonSerializer serializer)
        {
            object target = null;
            if (reader.TokenType != JsonToken.Null)
            {
                // Initialize and populate the initial object
                target = Activator.CreateInstance(objectType);
                var jObject = JObject.Load(reader);
                serializer.Populate(jObject.CreateReader(), target);
                
                // Check if the 'type' property is present to the JSON
                JToken mediaTypeJToken;
                if (jObject.TryGetValue(TYPE_KEY, out mediaTypeJToken))
                {
                    // Find the document property
                    var documentProperty =
                        objectType
                            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .First(p => p.PropertyType == typeof(Document));

                    var documentPropertyName = documentProperty.Name.ToCamelCase();
                    var mediaType = mediaTypeJToken.ToObject<MediaType>();

                    // Create the document instance
                    Document document;

                    Type documentType;
                    if (TypeUtil.TryGetTypeForMediaType(mediaType, out documentType))
                    {
                        if (mediaType.IsJson)
                        {
                            document = (Document) Activator.CreateInstance(documentType);
                            if (jObject[documentPropertyName] != null)
                            {
                                var resourceReader = jObject[documentPropertyName].CreateReader();
                                document = (Document)serializer.Deserialize(resourceReader, documentType);
                            }
                        }
                        else if (jObject[documentPropertyName] != null)
                        {
                            var parseFunc = TypeUtil.GetParseFuncForType(documentType);
                            document = (Document) parseFunc(jObject[documentPropertyName].ToString());
                        }
                        else
                        {
                            document = (Document) Activator.CreateInstance(documentType);
                        }
                    }
                    else
                    {
                        if (mediaType.IsJson)
                        {
                            var contentJsonObject = jObject[documentPropertyName] as IDictionary<string, JToken>;
                            if (contentJsonObject != null)
                            {
                                var contentDictionary = contentJsonObject.ToDictionary(k => k.Key, v => GetTokenValue(v.Value));
                                document = new JsonDocument(contentDictionary, mediaType);
                            }
                            else
                            {
                                throw new ArgumentException("The property is not a JSON");
                            }
                        }
                        else
                        {
                            document = new PlainDocument(jObject[documentPropertyName].ToString(), mediaType);
                        }
                    }

                    documentProperty.SetValue(target, document);
                }

            }

            return target;
        }



        private object GetTokenValue(JToken token)
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

    }
}
