using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lime.Protocol.Serialization.SystemTextJson.Converters
{
    /// <summary>
    /// Base class for converters that serialize types as strings using their <see cref="object.ToString"/> method
    /// and deserialize using a factory method.
    /// </summary>
    /// <typeparam name="T">The type to convert.</typeparam>
    public abstract class StringBasedJsonConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                return value != null ? CreateInstance(value) : default;
            }
            return default;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value != null)
                writer.WriteStringValue(value.ToString());
            else
                writer.WriteNullValue();
        }

        protected abstract T CreateInstance(string value);
    }
}
