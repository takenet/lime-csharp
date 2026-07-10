using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lime.Protocol.Serialization.SystemTextJson.Converters
{
    /// <summary>
    /// A <see cref="JsonConverterFactory"/> that handles any concrete type containing both a
    /// <see cref="Document"/>-typed property and a <see cref="MediaType"/>-typed property named "type".
    /// This covers <see cref="Message"/> (Content), <see cref="Command"/> (Resource), and <see cref="DocumentContainer"/> (Value).
    /// </summary>
    public class DocumentContainerJsonConverter : JsonConverterFactory
    {
        private static readonly Dictionary<Type, bool> _canConvertCache = new Dictionary<Type, bool>();
        private static readonly object _syncRoot = new object();
        private readonly IDocumentTypeResolver _documentTypeResolver;

        public DocumentContainerJsonConverter(IDocumentTypeResolver documentTypeResolver)
        {
            _documentTypeResolver = documentTypeResolver;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            if (typeToConvert.GetTypeInfo().IsAbstract) return false;

            lock (_syncRoot)
            {
                if (_canConvertCache.TryGetValue(typeToConvert, out var cached))
                {
                    return cached;
                }

                var properties = typeToConvert.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var canConvert =
                    properties.Any(p => p.PropertyType == typeof(Document)) &&
                    properties.Any(p =>
                        p.Name.Equals(DocumentContainer.TYPE_KEY, StringComparison.OrdinalIgnoreCase) &&
                        p.PropertyType == typeof(MediaType));

                _canConvertCache[typeToConvert] = canConvert;
                return canConvert;
            }
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(DocumentContainerJsonConverterInner<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType, _documentTypeResolver);
        }
    }

    internal sealed class DocumentContainerJsonConverterInner<T> : JsonConverter<T> where T : class
    {
        private static readonly PropertyInfo _documentProperty;
        private static readonly PropertyInfo _mediaTypeProperty;
        private static readonly string _documentPropertyJsonName;
        private static readonly List<(PropertyInfo Property, string JsonName, bool EmitDefault)> _properties;

        private readonly IDocumentTypeResolver _documentTypeResolver;

        static DocumentContainerJsonConverterInner()
        {
            var allProps = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            _documentProperty = allProps.First(p => p.PropertyType == typeof(Document));
            _mediaTypeProperty = allProps.First(p =>
                p.Name.Equals(DocumentContainer.TYPE_KEY, StringComparison.OrdinalIgnoreCase) &&
                p.PropertyType == typeof(MediaType));

            _documentPropertyJsonName = _documentProperty.Name.ToCamelCase();

            _properties = new List<(PropertyInfo, string, bool)>();
            foreach (var prop in allProps)
            {
                if (!prop.CanRead) continue;

                var dataMember = prop.GetCustomAttribute<DataMemberAttribute>();
                var jsonName = dataMember?.Name ?? prop.Name.ToCamelCase();
                var emitDefault = dataMember?.EmitDefaultValue ?? true;

                _properties.Add((prop, jsonName, emitDefault));
            }
        }

        public DocumentContainerJsonConverterInner(IDocumentTypeResolver documentTypeResolver)
        {
            _documentTypeResolver = documentTypeResolver;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            using var doc = System.Text.Json.JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var instance = CreateInstance();

            // First pass: populate all non-document properties
            foreach (var (prop, jsonName, _) in _properties)
            {
                if (prop == _documentProperty) continue;
                if (!prop.CanWrite) continue;

                if (root.TryGetProperty(jsonName, out var propElement))
                {
                    try
                    {
                        var value = JsonSerializer.Deserialize(propElement.GetRawText(), prop.PropertyType, options);
                        prop.SetValue(instance, value);
                    }
                    catch (JsonException) { }
                }
            }

            // Second pass: deserialize the Document property using MediaType
            if (root.TryGetProperty(DocumentContainer.TYPE_KEY, out var typeElement) &&
                typeElement.ValueKind != JsonValueKind.Null)
            {
                var mediaType = JsonSerializer.Deserialize<MediaType>(typeElement.GetRawText(), options);
                if (mediaType != null && root.TryGetProperty(_documentPropertyJsonName, out var documentElement))
                {
                    var document = DocumentHelper.DeserializeDocument(
                        documentElement, mediaType, options, _documentTypeResolver);
                    _documentProperty.SetValue(instance, document);
                }
            }

            return instance;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var (prop, jsonName, emitDefault) in _properties)
            {
                var propValue = prop.GetValue(value);
                if (propValue == null) continue;

                if (!emitDefault)
                {
                    var defaultValue = prop.PropertyType.IsValueType
                        ? Activator.CreateInstance(prop.PropertyType)
                        : null;
                    if (propValue.Equals(defaultValue)) continue;
                }

                writer.WritePropertyName(jsonName);

                if (prop == _documentProperty && propValue is Document document)
                {
                    DocumentHelper.SerializeDocument(writer, document, options);
                }
                else
                {
                    JsonSerializer.Serialize(writer, propValue, prop.PropertyType, options);
                }
            }

            writer.WriteEndObject();
        }

        private static T CreateInstance()
        {
            if (typeof(T) == typeof(Message)) return (T)(object)new Message();
            if (typeof(T) == typeof(Command)) return (T)(object)new Command(null);
            return Activator.CreateInstance<T>();
        }
    }
}
