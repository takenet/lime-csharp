using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Lime.Protocol.Serialization.Newtonsoft.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Lime.Protocol.Serialization.Newtonsoft
{
    /// <summary>
    /// Serializes using the Newtonsoft.Json library.
    /// </summary>
    /// <seealso cref="IEnvelopeSerializer" />
    public class EnvelopeSerializer : IEnvelopeSerializer
    {
        private readonly Lazy<JsonSerializer> _serializer;

        public EnvelopeSerializer(IDocumentTypeResolver documentTypeResolver)
        {
            if (documentTypeResolver == null) throw new ArgumentNullException(nameof(documentTypeResolver));

            Settings = CreateSettings(documentTypeResolver);
            _serializer = new Lazy<JsonSerializer>(() => JsonSerializer.Create(Settings));
        }

        public JsonSerializerSettings Settings { get; }

        /// <summary>
        /// Gets the <see cref="JsonSerializer"/> used for serialization and deserialization of envelopes.
        /// This property is lazy. The construction of the <see cref="JsonSerializer"/> instance will only
        /// happen after the first invocation of the <see langword="get"/> accessor.
        /// </summary>
        public JsonSerializer Serializer => _serializer.Value;

        /// <summary>
        /// Serialize an envelope to a string.
        /// </summary>
        public string Serialize(Envelope envelope)
        {
            return JsonConvert.SerializeObject(envelope, Formatting.None, Settings);
        }

        /// <summary>
        /// Deserialize an envelope from a string.
        /// </summary>
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

        /// <summary>
        /// Adds the provided <paramref name="jsonConverter"/> to the list of converters used by the underlying serializer.
        /// </summary>
        /// <param name="jsonConverter">The <see cref="JsonConverter"/> to be added.</param>
        /// <param name="ignoreDuplicates">Whether the provided <paramref name="jsonConverter"/> should be added when there is already one instance of that converter type.</param>
        /// <returns><see langword="true"/> if the element was added to the list. Otherwise, <see langword="false"/></returns>
        /// <exception cref="InvalidOperationException">Thrown when invoked after the serializer has already been constructed.</exception>
        /// <remarks>
        /// If the catch-all <see cref="DocumentJsonConverter"/> is present in the list, the provided <paramref name="jsonConverter"/> will be inserted before it.
        /// </remarks>
        public bool TryAddConverter(JsonConverter jsonConverter, bool ignoreDuplicates = true)
        {
            if (_serializer.IsValueCreated)
            {
                throw new InvalidOperationException("The serializer has already been constructed.");
            }

            if (ignoreDuplicates && Settings.Converters.Any(c => c.GetType() == jsonConverter.GetType()))
            {
                return false;
            }

            var converters = (List<JsonConverter>)Settings.Converters;
            var catchAllConverterIndex = converters.FindIndex(c => c.GetType() == typeof(DocumentJsonConverter));
            if (catchAllConverterIndex != -1)
            {
                converters.Insert(catchAllConverterIndex, jsonConverter);
            }
            else
            {
                converters.Add(jsonConverter);
            }

            return true;
        }

        internal static JsonSerializerSettings CreateSettings(IDocumentTypeResolver documentTypeResolver)
        {
            var converters = new List<JsonConverter>
            {
                new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() },
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

            var jsonSerializerSettings = new JsonSerializerSettings
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