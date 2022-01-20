using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Lime.Protocol.Serialization.Newtonsoft.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Lime.Protocol.Serialization.Newtonsoft
{
    /// <summary>
    /// Serializes using the Newtonsoft.Json library.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Serialization.IEnvelopeSerializer" />
    public class EnvelopeSerializer : IEnvelopeSerializer
    {
        public EnvelopeSerializer(IDocumentTypeResolver documentTypeResolver)
        {
            if (documentTypeResolver == null) throw new ArgumentNullException(nameof(documentTypeResolver));

            Settings = CreateSettings(documentTypeResolver);
            Serializer = JsonSerializer.Create(Settings);
        }

        public JsonSerializerSettings Settings { get; }

        public JsonSerializer Serializer { get; }

        /// <summary>
        /// Serialize an envelope to a string.
        /// </summary>
        /// <param name = "envelope"></param>
        /// <returns></returns>
        public string Serialize(Envelope envelope)
        {
            return JsonConvert.SerializeObject(envelope, Formatting.None, Settings);
        }

        /// <summary>
        /// Deserialize an envelope from a string.
        /// </summary>
        /// <param name = "envelopeString"></param>
        /// <returns></returns>
        /// <exception cref = "System.ArgumentException">JSON string is not a valid envelope</exception>
        public Envelope Deserialize(string envelopeString)
        {
            var jObject = JObject.Parse(envelopeString);
            
            if (jObject.Property(Message.CONTENT_KEY) != null)
            {
                return jObject.ToObject<Message>(Serializer);
            }
            if (jObject.Property(Notification.EVENT_KEY) != null)
            {
                return jObject.ToObject<Notification>(Serializer);
            }
            if (jObject.Property(Command.METHOD_KEY) != null)
            {
                return jObject.ToObject<Command>(Serializer);
            }
            if (jObject.Property(Session.STATE_KEY) != null)
            {
                return jObject.ToObject<Session>(Serializer);
            }

            throw new ArgumentException("JSON string is not a valid envelope", nameof(envelopeString));
        }

        public bool TryAddConverter(JsonConverter jsonConverter, bool checkForDuplicate = false)
        {
            if (checkForDuplicate && Settings.Converters.Any(c => c.GetType() == jsonConverter.GetType()))
            {
                return false;
            }

            Settings.Converters.Add(jsonConverter);
            return true;
        }

        internal static JsonSerializerSettings CreateSettings(IDocumentTypeResolver documentTypeResolver)
        {
            var converters = new List<JsonConverter>
            {
                new StringEnumConverter { CamelCaseText = false },
                new IdentityJsonConverter(),
                new NodeJsonConverter(),
                new LimeUriJsonConverter(),
                new MediaTypeJsonConverter(),
                new SessionJsonConverter(),
                new AuthenticationJsonConverter(),
                new DocumentContainerJsonConverter(documentTypeResolver),
                new DocumentCollectionJsonConverter(documentTypeResolver),
                new IsoDateTimeConverter
                {
                    DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ",
                    DateTimeStyles = DateTimeStyles.AdjustToUniversal
                }
            };

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Converters = converters
            };

            // This needs to be added last, since it's a "catch-all" document converter
            converters.Add(new DocumentJsonConverter(jsonSerializerSettings));
            return jsonSerializerSettings;
        }
    }
}