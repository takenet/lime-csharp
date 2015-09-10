using Lime.Protocol.Security;
using Newtonsoft.Json;
using System;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    class AuthenticationJsonConverter : JsonConverter
    {
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof (Authentication).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            return null;
        }

        public override void WriteJson(global::Newtonsoft.Json.JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}