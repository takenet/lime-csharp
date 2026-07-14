using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lime.Protocol.Serialization.SystemTextJson.Converters
{
    /// <summary>
    /// A <see cref="JsonConverter{T}"/> for <see cref="DocumentCollection"/> that correctly
    /// deserializes each item using the collection's <see cref="DocumentCollection.ItemType"/>.
    /// </summary>
    public class DocumentCollectionJsonConverter : JsonConverter<DocumentCollection>
    {
        private readonly IDocumentTypeResolver _documentTypeResolver;

        public DocumentCollectionJsonConverter(IDocumentTypeResolver documentTypeResolver)
        {
            _documentTypeResolver = documentTypeResolver;
        }

        public override DocumentCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            using var doc = System.Text.Json.JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var instance = new DocumentCollection();

            if (root.TryGetProperty(DocumentCollection.ITEM_TYPE_KEY, out var itemTypeElement) &&
                itemTypeElement.ValueKind != JsonValueKind.Null)
            {
                instance.ItemType = JsonSerializer.Deserialize<MediaType>(itemTypeElement.GetRawText(), options);
            }

            if (root.TryGetProperty(DocumentCollection.TOTAL_KEY, out var totalElement) &&
                totalElement.ValueKind != JsonValueKind.Null)
            {
                instance.Total = totalElement.GetInt32();
            }

            if (root.TryGetProperty(DocumentCollection.ITEMS_KEY, out var itemsElement) &&
                itemsElement.ValueKind == JsonValueKind.Array &&
                instance.ItemType != null)
            {
                var items = new Document[itemsElement.GetArrayLength()];
                var i = 0;
                foreach (var itemElement in itemsElement.EnumerateArray())
                {
                    items[i++] = DocumentHelper.DeserializeDocument(
                        itemElement, instance.ItemType, options, _documentTypeResolver);
                }
                instance.Items = items;
            }

            return instance;
        }

        public override void Write(Utf8JsonWriter writer, DocumentCollection value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            if (value.ItemType != null)
            {
                writer.WritePropertyName(DocumentCollection.ITEM_TYPE_KEY);
                JsonSerializer.Serialize(writer, value.ItemType, options);
            }

            if (value.Total != 0)
            {
                writer.WriteNumber(DocumentCollection.TOTAL_KEY, value.Total);
            }

            if (value.Items != null)
            {
                writer.WritePropertyName(DocumentCollection.ITEMS_KEY);
                writer.WriteStartArray();
                foreach (var item in value.Items)
                {
                    if (item == null)
                    {
                        writer.WriteNullValue();
                        continue;
                    }
                    DocumentHelper.SerializeDocument(writer, item, options);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }
}
