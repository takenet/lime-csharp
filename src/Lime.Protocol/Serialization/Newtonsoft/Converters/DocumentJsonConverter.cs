using Newtonsoft.Json;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    public class DocumentJsonConverter : JsonConverter
    {
        private readonly global::Newtonsoft.Json.JsonSerializer _alternativeSerializer;

        public DocumentJsonConverter(JsonSerializerSettings settings)
        {
            _alternativeSerializer = global::Newtonsoft.Json.JsonSerializer.Create(settings);
        }

        public override bool CanRead => true;

        public override bool CanWrite => true;        
        
        public override bool CanConvert(Type objectType)
        {
            return typeof(Document).IsAssignableFrom(objectType) && !typeof(DocumentCollection).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            if (objectType.GetTypeInfo().IsAbstract)
            {
                // The serialization is made by the container class (Message or Command)
                return null;
            }

            var instance = Activator.CreateInstance(objectType);
            serializer.Populate(reader, instance);
            return instance;
        }

        public override void WriteJson(JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            var document = value as Document;
            if (document != null)
            {                                                                
                if (document.GetMediaType().IsJson)
                {
                    // TODO: Any document inside the value will not be correct handled.
                    _alternativeSerializer.Serialize(writer, document);                    
                }
                else
                {
                    writer.WriteValue(document.ToString());
                }
            }
            else
            {
                writer.WriteNull();
            }
        }
    }
}