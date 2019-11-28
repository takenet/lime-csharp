using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lime.Protocol.Serialization.Json.Converters
{
    public abstract class StringJsonConverterBase<T> : JsonConverter<T> where T : class
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) 
            => Parse(reader.GetString());

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());

        protected abstract T Parse(string value);
    }
}