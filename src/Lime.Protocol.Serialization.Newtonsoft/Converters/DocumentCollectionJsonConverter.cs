using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    public class DocumentCollectionJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DocumentCollection);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            var result = serializer.Deserialize<JsonDocumentCollection>(reader);
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            var collection = (DocumentCollection)value;
            var jsonCollection = new JsonDocumentCollection(collection);
            serializer.Serialize(writer, jsonCollection);
        }
    }

    [JsonObject]
    internal class JsonDocumentCollection : DocumentCollection
    {
        internal JsonDocumentCollection(DocumentCollection collection)
        {
            this.Items = collection.Items;
            this.ItemType = collection.ItemType;
            this.Total = collection.Total;
        }
    }
}
