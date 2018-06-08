using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    public class CommandJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);            
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(Command);
    }
}
