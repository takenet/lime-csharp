using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Lime.Protocol.Serialization.SystemTextJson
{
    /// <summary>
    /// Extension methods for <see cref="JsonElement"/>.
    /// </summary>
    public static class JsonElementExtensions
    {
        /// <summary>
        /// Converts a <see cref="JsonElement"/> that represents a JSON object
        /// into a dictionary of property names to .NET values.
        /// </summary>
        /// <param name="element">The <see cref="JsonElement"/> to convert. Must be of <see cref="JsonValueKind.Object"/>.</param>
        /// <returns>A dictionary containing the property names and values of the JSON object.</returns>
        public static IDictionary<string, object> ConvertToDictionary(this JsonElement element)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in element.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.GetElementValue();
            }
            return dict;
        }

        /// <summary>
        /// Gets the .NET value represented by a <see cref="JsonElement"/>,
        /// converting strings, numbers, booleans, arrays and objects recursively.
        /// </summary>
        /// <param name="element">The <see cref="JsonElement"/> to convert.</param>
        /// <returns>The .NET value that represents the JSON element.</returns>
        public static object GetElementValue(this JsonElement element)
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
                    return element.EnumerateArray().Select(e => e.GetElementValue()).ToArray();
                case JsonValueKind.Object:
                    return element.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetElementValue());
                default:
                    return element.GetRawText();
            }
        }

        /// <summary>
        /// Gets the string value represented by a <see cref="JsonElement"/>.
        /// When the element is a JSON string, its string value is returned;
        /// otherwise the raw JSON text is returned.
        /// </summary>
        /// <param name="element">The <see cref="JsonElement"/> to convert.</param>
        /// <returns>The string representation of the JSON element.</returns>
        public static string GetStringValue(this JsonElement element)
        {
            return element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : element.GetRawText();
        }
    }
}