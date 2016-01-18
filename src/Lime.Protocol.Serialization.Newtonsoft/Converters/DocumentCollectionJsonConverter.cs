using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            var instance = new DocumentCollection();
            var jObject = JObject.Load(reader);
            if (jObject[DocumentCollection.ITEM_TYPE_KEY] != null)
            {
                instance.ItemType = jObject[DocumentCollection.ITEM_TYPE_KEY].ToObject<MediaType>();
            }

            if (jObject[DocumentCollection.ITEMS_KEY] != null && instance.ItemType != null)
            {
                Type itemType;
                if (TypeUtil.TryGetTypeForMediaType(instance.ItemType, out itemType))
                {
                    var items = jObject[DocumentCollection.ITEMS_KEY];
                    if (items.Type == JTokenType.Array)
                    {
                        var itemsArray = (JArray)items;
                        instance.Items = new Document[itemsArray.Count];
                        for (int i = 0; i < itemsArray.Count; i++)
                        {
                            var item = itemsArray[i];
                            instance.Items[i] = (Document)Activator.CreateInstance(itemType);
                            serializer.Populate(item.CreateReader(), instance.Items[i]);
                        }
                    }
                }
            }

            if (jObject[DocumentCollection.TOTAL_KEY] != null)
            {
                instance.Total = jObject[DocumentCollection.TOTAL_KEY].ToObject<int>();
            }

            return instance;
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
            Items = collection.Items;
            ItemType = collection.ItemType;
            Total = collection.Total;
        }
    }
}
