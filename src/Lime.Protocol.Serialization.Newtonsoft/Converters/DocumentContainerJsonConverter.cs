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
        private readonly global::Newtonsoft.Json.JsonSerializer _alternativeSerializer;

        public DocumentContainerJsonConverter(JsonSerializerSettings settings)
        {
            settings.ContractResolver = new IgnoreDocumentContractResolver();
            _alternativeSerializer = global::Newtonsoft.Json.JsonSerializer.Create(settings);            
        }

        public const string TYPE_KEY = "type";        

        public override void WriteJson(JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            var documentProperty =
                value
                    .GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .First(p => p.PropertyType == typeof(Document));

            JObject.FromObject(value, _alternativeSerializer);            

            var document = documentProperty.GetValue(value) as Document;
            _alternativeSerializer.Serialize(writer, value);            
                        
            // Now handle the document property accordingly
            if (document != null)
            {
                var mediaType = document.GetMediaType();
                writer.WritePropertyName(documentProperty.Name.ToCamelCase());
                if (mediaType.IsJson)
                {
                    serializer.Serialize(writer, document);
                }
                else
                {
                    writer.WriteValue(document.ToString());
                }
            }            
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
                _alternativeSerializer.Populate(jObject.CreateReader(), target);
                
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
                                document = (Document)_alternativeSerializer.Deserialize(resourceReader, documentType);
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

        public override bool CanConvert(Type objectType)
        {
            if (objectType.IsAbstract) return false;

            var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return 
                properties.Any(p => p.PropertyType == typeof (Document)) &&
                properties.Any(p => p.Name.Equals(TYPE_KEY, StringComparison.OrdinalIgnoreCase) && p.PropertyType == typeof(MediaType));
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

        private class IgnoreDocumentContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                return base.CreateProperties(type, memberSerialization)
                    .Where(p => !typeof(Document).IsAssignableFrom(p.PropertyType))
                    .ToList();
            }
        }
    }
}
