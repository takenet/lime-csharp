using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lime.Protocol.Serialization.SystemTextJson.Converters
{
    /// <summary>
    /// A catch-all <see cref="JsonConverter{T}"/> for <see cref="Document"/> subclasses
    /// that are not handled by more specific converters.
    /// </summary>
    public class DocumentJsonConverter : JsonConverterFactory
    {
        private readonly IDocumentTypeResolver _documentTypeResolver;
        private readonly JsonSerializerOptions _options;

        public DocumentJsonConverter(IDocumentTypeResolver documentTypeResolver, JsonSerializerOptions options)
        {
            _documentTypeResolver = documentTypeResolver;
            _options = options;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Document).IsAssignableFrom(typeToConvert) &&
                   !typeof(DocumentCollection).IsAssignableFrom(typeToConvert);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(DocumentJsonConverterInner<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType, _documentTypeResolver, _options);
        }
    }

    internal sealed class DocumentJsonConverterInner<T> : JsonConverter<T> where T : Document
    {
        private readonly IDocumentTypeResolver _documentTypeResolver;
        private readonly JsonSerializerOptions _options;

        public DocumentJsonConverterInner(IDocumentTypeResolver documentTypeResolver, JsonSerializerOptions options)
        {
            _documentTypeResolver = documentTypeResolver;
            _options = options;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert.GetTypeInfo().IsAbstract)
            {
                // Abstract types are handled by their container (Message or Command)
                reader.Skip();
                return null;
            }

            using var doc = System.Text.Json.JsonDocument.ParseValue(ref reader);
            var instance = (T)Activator.CreateInstance(typeToConvert);

            // For dictionary-based documents (e.g. JsonDocument), populate the dictionary directly
            if (instance is IDictionary<string, object> dict)
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var jsonProp in doc.RootElement.EnumerateObject())
                    {
                        dict[jsonProp.Name] = jsonProp.Value.GetElementValue();
                    }
                }
                return instance;
            }

            // Build a case-insensitive lookup so that properties with non-camelCase JSON names
            // (e.g. "NullableDouble" in raw JSON) are still matched to their C# counterpart.
            var jsonProperties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var jsonProp in doc.RootElement.EnumerateObject())
            {
                jsonProperties[jsonProp.Name] = jsonProp.Value;
            }

            // Populate writable, non-indexed properties
            foreach (var prop in typeToConvert.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanWrite) continue;
                if (prop.GetIndexParameters().Length > 0) continue;

                var jsonName = _options.PropertyNamingPolicy?.ConvertName(prop.Name) ?? prop.Name;
                if (jsonProperties.TryGetValue(jsonName, out var propElement))
                {
                    try
                    {
                        var value = JsonSerializer.Deserialize(propElement.GetRawText(), prop.PropertyType, options);
                        prop.SetValue(instance, value);
                    }
                    catch (JsonException) { }
                }
            }

            return instance;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            // For dictionary-based documents (e.g. JsonDocument), serialize key-value pairs directly
            if (value is IDictionary<string, object> dict)
            {
                writer.WriteStartObject();
                foreach (var kv in dict)
                {
                    writer.WritePropertyName(kv.Key);
                    if (kv.Value == null)
                        writer.WriteNullValue();
                    else
                        JsonSerializer.Serialize(writer, kv.Value, kv.Value.GetType(), _options);
                }
                writer.WriteEndObject();
                return;
            }

            if (value.GetMediaType().IsJson)
            {
                writer.WriteStartObject();
                foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!prop.CanRead) continue;
                    if (prop.GetIndexParameters().Length > 0) continue;
                    var propValue = prop.GetValue(value);
                    if (propValue == null) continue;

                    var jsonName = options.PropertyNamingPolicy?.ConvertName(prop.Name) ?? prop.Name;
                    writer.WritePropertyName(jsonName);
                    JsonSerializer.Serialize(writer, propValue, prop.PropertyType, _options);
                }
                writer.WriteEndObject();
            }
            else
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }
}
