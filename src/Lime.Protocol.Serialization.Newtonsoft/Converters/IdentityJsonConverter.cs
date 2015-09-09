using Newtonsoft.Json;
using System;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    class IdentityJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (Identity);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var tokenValue = reader.Value.ToString();
                return Identity.Parse(tokenValue);
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
                Identity identity = (Identity)value;
                writer.WriteValue(identity.ToString());
            }
            else
            {
                writer.WriteNull();
            }
        }
    }
}