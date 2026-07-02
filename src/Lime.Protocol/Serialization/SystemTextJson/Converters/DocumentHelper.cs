using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Lime.Protocol.Serialization.SystemTextJson.Converters
{
    /// <summary>
    /// Helper methods for serializing and deserializing <see cref="Document"/> instances
    /// using <see cref="System.Text.Json"/>.
    /// </summary>
    internal static class DocumentHelper
    {
        /// <summary>
        /// Deserializes a <see cref="Document"/> from a <see cref="JsonElement"/>, using the provided <paramref name="mediaType"/>
        /// to determine the concrete document type.
        /// </summary>
        internal static Document DeserializeDocument(
            JsonElement element,
            MediaType mediaType,
            JsonSerializerOptions options,
            IDocumentTypeResolver documentTypeResolver)
        {
            if (documentTypeResolver.TryGetTypeForMediaType(mediaType, out var documentType))
            {
                // DictionaryDocument types are better handled as generic JSON below
                if (!typeof(DictionaryDocument<string, object>).IsAssignableFrom(documentType))
                {
                    try
                    {
                        if (mediaType.IsJson)
                        {
                            return (Document)JsonSerializer.Deserialize(
                                element.GetRawText(), documentType, options);
                        }
                        else
                        {
                            var text = GetStringValue(element);
                            var parseFunc = TypeUtilEx.GetParseFuncForType(documentType);
                            return (Document)parseFunc(text);
                        }
                    }
                    catch (JsonException) { }
                    catch (ArgumentException) { }
                    catch (TypeLoadException) { }
                }
            }

            // Fallback: generic JSON or plain text handling
            if (mediaType.IsJson)
            {
                var dict = ConvertElementToDictionary(element);
                return new JsonDocument(dict, mediaType);
            }

            return new PlainDocument(GetStringValue(element), mediaType);
        }

        /// <summary>
        /// Serializes a <see cref="Document"/> to a <see cref="Utf8JsonWriter"/>.
        /// Non-JSON documents are written as strings; JSON documents are written as JSON objects.
        /// </summary>
        internal static void SerializeDocument(Utf8JsonWriter writer, Document document, JsonSerializerOptions options)
        {
            if (!document.GetMediaType().IsJson)
            {
                writer.WriteStringValue(document.ToString());
            }
            else
            {
                JsonSerializer.Serialize(writer, document, document.GetType(), options);
            }
        }

        private static string GetStringValue(JsonElement element)
        {
            return element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : element.GetRawText();
        }

        private static IDictionary<string, object> ConvertElementToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in element.EnumerateObject())
            {
                dict[prop.Name] = GetElementValue(prop.Value);
            }
            return dict;
        }

        internal static object GetElementValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out var longValue)) return longValue;
                    return element.GetDouble();
                case JsonValueKind.Array:
                    return element.EnumerateArray().Select(GetElementValue).ToArray();
                case JsonValueKind.Object:
                    return element.EnumerateObject().ToDictionary(p => p.Name, p => GetElementValue(p.Value));
                default:
                    return element.GetRawText();
            }
        }
    }
}
