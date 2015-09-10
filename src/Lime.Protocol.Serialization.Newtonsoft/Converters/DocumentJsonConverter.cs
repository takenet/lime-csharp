using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    class DocumentJsonConverter : JsonConverter
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
            return typeof(Document).IsAssignableFrom(objectType) && !typeof(DocumentCollection).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            if (objectType.IsAbstract)
            {
                // The serialization is made by the
                // container class (Message or Command)
                return null;
            }
            else
            {
                var instance = Activator.CreateInstance(objectType);
                serializer.Populate(reader, instance);
                return instance;
            }
        }

        public override void WriteJson(global::Newtonsoft.Json.JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}