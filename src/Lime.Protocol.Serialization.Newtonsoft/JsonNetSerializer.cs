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
    public class JsonNetSerializer : IEnvelopeSerializer
    {
        private static object _syncRoot = new object();

        static JsonNetSerializer()
        {
            JsonConvert.DefaultSettings = () => Settings;
            _serializer = global::Newtonsoft.Json.JsonSerializer.Create(Settings);
        }

        private static JsonSerializerSettings _settings;
        private static readonly global::Newtonsoft.Json.JsonSerializer _serializer;

        public static JsonSerializerSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    lock (_syncRoot)
                    {
                        if (_settings == null)
                        {
                            var converters = new List<JsonConverter>
                            {
                                new StringEnumConverter {CamelCaseText = false},
                                new IdentityJsonConverter(),
                                new NodeJsonConverter(),
                                new LimeUriJsonConverter(),
                                new MediaTypeJsonConverter(),
                                new UriJsonConverter(),
                                new SessionJsonConverter(),
                                new AuthenticationJsonConverter(),
                                new DocumentContainerJsonConverter(),
                                new DocumentCollectionJsonConverter(),
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

                            _settings = new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore,
                                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                                Converters = converters
                            };
                        }
                    }
                }

                return _settings;
            }
        }

        #region IEnvelopeSerializer Members

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

            if (jObject.Property("content") != null)
            {
                return jObject.ToObject<Message>(_serializer);
            }
            if (jObject.Property("event") != null)
            {
                return jObject.ToObject<Notification>(_serializer);
            }
            if (jObject.Property("method") != null)
            {
                return jObject.ToObject<Command>(_serializer);
            }
            if (jObject.Property("state") != null)
            {
                return jObject.ToObject<Session>(_serializer);
            }
            throw new ArgumentException("JSON string is not a valid envelope");
        }

        #endregion
    }
}