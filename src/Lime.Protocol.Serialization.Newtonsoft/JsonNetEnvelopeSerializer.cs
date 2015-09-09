using Lime.Protocol.Serialization.Newtonsoft.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lime.Protocol.Serialization.Newtonsoft
{
    public class JsonNetEnvelopeSerializer : IEnvelopeSerializer
    {
        private readonly global::Newtonsoft.Json.JsonSerializer _jsonSerializer;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public JsonNetEnvelopeSerializer()
        {
            _jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            };
            new JsonConverter[]
            {
                new IsoDateTimeConverter
                {
                    DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ",
                    DateTimeStyles = DateTimeStyles.AdjustToUniversal
                },
                new StringEnumConverter { CamelCaseText = false },
                new StringBasedTypesJsonConverter(),
                new DocumentCollectionJsonConverter()
            }.ForEach(c => _jsonSerializerSettings.Converters.Add(c));

            _jsonSerializer = global::Newtonsoft.Json.JsonSerializer.Create(_jsonSerializerSettings);
        }

        public string Serialize(Envelope envelope)
        {
            return JsonConvert.SerializeObject(envelope, _jsonSerializerSettings);
        }

        public Envelope Deserialize(string envelopeString)
        {
            var jObject = JObject.Parse(envelopeString);

            if (jObject.Property("content") != null)
            {
                return jObject.ToObject<Message>(_jsonSerializer);
            }
            else if (jObject.Property("event") != null)
            {
                return jObject.ToObject<Notification>(_jsonSerializer);
            }
            else if (jObject.Property("method") != null)
            {
                return jObject.ToObject<Command>(_jsonSerializer);
            }
            else if (jObject.Property("state") != null)
            {
                return jObject.ToObject<Session>(_jsonSerializer);
            }
            else
            {
                throw new ArgumentException("JSON string is not a valid envelope");
            }
        }
    }
}
