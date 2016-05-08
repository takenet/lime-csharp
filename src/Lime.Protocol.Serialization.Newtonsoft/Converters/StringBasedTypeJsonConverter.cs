using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    public abstract class StringBasedTypeJsonConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(T);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var tokenValue = reader.Value.ToString();
                return CreateInstance(tokenValue);
            }
            else
            {
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value != null)
            {
                var identity = (T)value;
                writer.WriteValue(identity.ToString());
            }
            else
            {
                writer.WriteNull();
            }
        }

        protected abstract T CreateInstance(string tokenValue);

    }
}
