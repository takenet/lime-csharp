using Newtonsoft.Json;
using System;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    class LimeUriJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (LimeUri);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var tokenValue = reader.Value.ToString();
                return LimeUri.Parse(tokenValue);
            }
            else
            {
                return null;
            }
        }

        public override void WriteJson(global::Newtonsoft.Json.JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value != null)
            {
                LimeUri identity = (LimeUri)value;
                writer.WriteValue(identity.ToString());
            }
            else
            {
                writer.WriteNull();
            }
        }
    }
}