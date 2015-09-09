using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    public class StringBasedTypesJsonConverter : JsonConverter
    {
        private static Type[] _handledTypes = {
            typeof(LimeUri),
            typeof(Node),
            typeof(Identity),
            typeof(MediaType),
            typeof(Uri)
        };

        public override bool CanConvert(Type objectType)
        {
            return _handledTypes.Any(t => t == objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            var value = reader.ReadAsString();

            if (objectType == typeof(LimeUri))
            {
                return new LimeUri(value);
            }

            if (objectType == typeof(Node))
            {
                return Node.Parse(value);
            }

            if (objectType == typeof(Identity))
            {
                return Identity.Parse(value);
            }

            if (objectType == typeof(MediaType))
            {
                return MediaType.Parse(value);
            }

            if (objectType == typeof(Uri))
            {
                return new Uri(value);
            }

            throw new ArgumentOutOfRangeException(nameof(objectType));
        }

        public override void WriteJson(JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
