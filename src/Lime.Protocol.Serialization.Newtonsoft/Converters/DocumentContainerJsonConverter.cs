using System;
using System.Collections.Concurrent;
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
        // TODO: Implement a cache
        private static readonly ConcurrentDictionary<Type, bool> CanConvertDictionary = new ConcurrentDictionary<Type, bool>();

        public override bool CanRead => true;

        public override bool CanWrite => true;        

        public override bool CanConvert(Type objectType)
        {
            // This implementation works with all classes that have a 'type' property among a typeof(Document) property, not only the DocumentContainer class.
            if (objectType.IsAbstract) return false;

            var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var canConvert = 
                properties.Any(p => p.PropertyType == typeof(Document)) &&
                properties.Any(p => p.Name.Equals(DocumentContainer.TYPE_KEY, StringComparison.OrdinalIgnoreCase) && p.PropertyType == typeof(MediaType));

            return canConvert;
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
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
                if (jObject.TryGetValue(DocumentContainer.TYPE_KEY, out mediaTypeJToken))
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

        public override void WriteJson(JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            // The container should be always a JSON
            var contract = serializer.ContractResolver.ResolveContract(value.GetType()) as JsonObjectContract;
            if (contract == null) return;
            
            var shouldStartObject = writer.WriteState == WriteState.Start || writer.WriteState == WriteState.Array;
            if (shouldStartObject) writer.WriteStartObject();
            
            foreach (var property in contract.Properties.Where(p => p.ShouldSerialize == null || p.ShouldSerialize(value)))
            {                
                var propertyValue = property.ValueProvider.GetValue(value);
                if (propertyValue == null) continue;

                if (property.DefaultValueHandling == DefaultValueHandling.Ignore &&
                    propertyValue.Equals(property.DefaultValue ?? property.PropertyType.GetDefaultValue()))
                {                    
                    continue;
                }

                writer.WritePropertyName(property.PropertyName);                

                var document = propertyValue as Document;
                if (document != null && !document.GetMediaType().IsJson)
                {
                    writer.WriteValue(document.ToString());
                }
                else
                {
                    serializer.Serialize(writer, propertyValue);
                }                
            }

            if (shouldStartObject) writer.WriteEndObject();
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