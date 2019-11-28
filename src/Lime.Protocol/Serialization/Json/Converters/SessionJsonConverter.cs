using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lime.Protocol.Serialization.Json.Converters
{
    public class SessionJsonConverter : JsonConverter<Session>
    {
        private readonly JsonSerializerOptions _options;

        public SessionJsonConverter(JsonSerializerOptions options)
        {
            _options = options;
        }
        
        public override Session Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            
            Session session = null;
            return session;
        }

        public override void Write(Utf8JsonWriter writer, Session value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, _options);
        }
    }
}