using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    class CommandJsonConverter : JsonConverter
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
            return objectType == typeof (Command);
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
                var command = new Command();
                serializer.Populate(jObject.CreateReader(), command);
                if (jObject[Command.TYPE_KEY] != null)
                {
                    var resourceMediaType = jObject[Command.TYPE_KEY].ToObject<MediaType>();
                    Type documentType;
                    if (TypeUtil.TryGetTypeForMediaType(resourceMediaType, out documentType))
                    {
                        if (resourceMediaType.IsJson)
                        {
                            command.Resource = (Document)Activator.CreateInstance(documentType);
                            if (jObject[Command.RESOURCE_KEY] != null)
                            {
                                var resourceReader = jObject[Command.RESOURCE_KEY].CreateReader();
                                command.Resource = (Document)serializer.Deserialize(resourceReader, documentType);
                            }
                        }
                        else if (jObject[Command.RESOURCE_KEY] != null)
                        {
                            var parseFunc = TypeUtil.GetParseFuncForType(documentType);
                            command.Resource = (Document)parseFunc(jObject[Command.RESOURCE_KEY].ToString());
                        }
                        else
                        {
                            command.Resource = (Document)Activator.CreateInstance(documentType);
                        }
                    }
                    else
                    {
                        if (resourceMediaType.IsJson)
                        {
                            var contentJsonObject = jObject[Command.RESOURCE_KEY] as IDictionary<string, JToken>;
                            if (contentJsonObject != null)
                            {
                                var contentDictionary = contentJsonObject.ToDictionary(k => k.Key, v => (object)v.Value.ToString());
                                command.Resource = new JsonDocument(contentDictionary, resourceMediaType);
                            }
                            else
                            {
                                throw new ArgumentException("The property is not a JSON");
                            }
                        }
                        else
                        {
                            command.Resource = new PlainDocument(jObject[Command.RESOURCE_KEY].ToString(), resourceMediaType);
                        }
                    }
                }

                target = command;
            }

            return target;
        }

        public override void WriteJson(global::Newtonsoft.Json.JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}