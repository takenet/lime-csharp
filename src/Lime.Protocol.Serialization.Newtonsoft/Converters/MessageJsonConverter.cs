using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Lime.Protocol.Serialization.Newtonsoft.Extensions;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    class MessageJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (Message);
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name = "reader">The <see cref = "T:Newtonsoft.Json.JsonReader"/> to read from.</param>
        /// <param name = "objectType">Type of the object.</param>
        /// <param name = "existingValue">The existing value of object being read.</param>
        /// <param name = "serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            object target = null;
            if (reader.TokenType != JsonToken.Null)
            {
                JObject jObject = JObject.Load(reader);
                if (jObject[Message.CONTENT_KEY] != null && jObject[Message.TYPE_KEY] != null)
                {
                    var message = new Message(Guid.Empty);
                    serializer.Populate(jObject.CreateReader(), message);
                    var contentMediaType = jObject[Message.TYPE_KEY].ToObject<MediaType>();
                    Type documentType;
                    if (TypeUtil.TryGetTypeForMediaType(contentMediaType, out documentType))
                    {
                        if (contentMediaType.IsJson)
                        {
                            message.Content = (Document)Activator.CreateInstance(documentType);
                            serializer.Populate(jObject[Message.CONTENT_KEY].CreateReader(), message.Content);
                        }
                        else
                        {
                            var parseFunc = TypeUtil.GetParseFuncForType(documentType);
                            message.Content = (Document)parseFunc(jObject[Message.CONTENT_KEY].ToString());
                        }
                    }
                    else
                    {
                        if (contentMediaType.IsJson)
                        {
                            var contentJsonObject = jObject[Message.CONTENT_KEY] as IDictionary<string, JToken>;
                            if (contentJsonObject != null)
                            {
                                var contentDictionary = contentJsonObject.ToDictionary(k => k.Key, v => GetTokenValue(v.Value));
                                message.Content = new JsonDocument(contentDictionary, contentMediaType);
                            }
                            else
                            {
                                throw new ArgumentException("The property is not a JSON");
                            }
                        }
                        else
                        {
                            message.Content = new PlainDocument(jObject[Message.CONTENT_KEY].ToString(), contentMediaType);
                        }
                    }

                    target = message;
                }
                else
                {
                    throw new ArgumentException("Invalid Message JSON");
                }
            }

            return target;
        }

        private object GetTokenValue(JToken token)
        {
            if (token is JValue)
            {
                return ((JValue)token).Value;
            }

            if (token is JArray)
            {
                return ((JArray)token)
                            .Select(t => GetTokenValue(t))
                            .ToArray();
            }

            if (token is JObject)
            {
                return ((IDictionary<string, JToken>)token).ToDictionary(k => k.Key, v => GetTokenValue(v.Value));
            }

            throw new ArgumentException("Unknown token type");
        }

        public override void WriteJson(JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            var message = (Message)value;
            writer.WriteStartObject();

            writer.WriteValueIfNotDefault(Envelope.ID_KEY, message.Id);
            writer.WriteValueIfNotDefaultAsString(Envelope.FROM_KEY, message.From);
            writer.WriteValueIfNotDefaultAsString(Envelope.TO_KEY, message.To);
            writer.WriteValueIfNotDefaultAsString(Envelope.PP_KEY, message.Pp);
            writer.WritePropertyName(Message.TYPE_KEY);
            writer.WriteValue(message.Type.ToString());

            writer.WritePropertyName(Message.CONTENT_KEY);
            if (message.Content is JsonDocument)
            {
                serializer.Serialize(writer, message.Content);
            }
            else
            {
                writer.WriteValue(message.Content.ToString());
            }

            if (message.Metadata != null)
            {
                writer.WritePropertyName(Envelope.METADATA_KEY);
                writer.WriteStartObject();
                foreach (var item in message.Metadata)
                {
                    writer.WritePropertyName(item.Key);
                    writer.WriteValue(item.Value);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }
    }
}