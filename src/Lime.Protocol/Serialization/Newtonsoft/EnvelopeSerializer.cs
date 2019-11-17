using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public class EnvelopeSerializer : IEnvelopeSerializer, IStreamEnvelopeSerializer
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
            return jObject.ToEnvelope(Serializer);
        }
        
        public Task SerializeAsync(Envelope envelope, Stream stream, CancellationToken cancellationToken)
        {
            using var streamWriter = new StreamWriter(stream);
            using var jsonWriter = new JsonTextWriter(streamWriter);
            Serializer.Serialize(jsonWriter, envelope);
            return Task.CompletedTask;
        }

        public Task<Envelope> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
        {
            using var streamReader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(streamReader);
            var jToken = JToken.ReadFrom(jsonReader);
            if (!(jToken is JObject jObject))
            {
                throw new ArgumentException("The JSON read from stream was not a valid object", nameof(stream));
            }
            return jObject.ToEnvelope(Serializer).AsCompletedTask();
        }

        internal static JsonSerializerSettings CreateSettings(IDocumentTypeResolver documentTypeResolver)
        {
            var converters = new List<JsonConverter>
            {
                new StringEnumConverter {CamelCaseText = false},
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
            converters.Add(new DocumentJsonConverter(
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    Converters = converters.ToList()
                }));

            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Converters = converters,
                Formatting = Formatting.None
            };
        }



    }

    public static class JObjectExtensions
    {
        public static Envelope ToEnvelope(this JObject jObject, JsonSerializer serializer)
        {
            if (jObject.Property(Message.CONTENT_KEY) != null)
            {
                return jObject.ToObject<Message>(serializer);
            }
            if (jObject.Property(Notification.EVENT_KEY) != null)
            {
                return jObject.ToObject<Notification>(serializer);
            }
            if (jObject.Property(Command.METHOD_KEY) != null)
            {
                return jObject.ToObject<Command>(serializer);
            }
            if (jObject.Property(Session.STATE_KEY) != null)
            {
                return jObject.ToObject<Session>(serializer);
            }

            throw new ArgumentException("JSON is not a valid envelope");
        }
    }
}