using Newtonsoft.Json;
using System;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    class DocumentJsonConverter : JsonConverter
    {
        public override bool CanWrite => false;        

        public override bool CanRead => true;

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

            var instance = Activator.CreateInstance(objectType);
            serializer.Populate(reader, instance);
            return instance;
        }

        public override void WriteJson(JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}