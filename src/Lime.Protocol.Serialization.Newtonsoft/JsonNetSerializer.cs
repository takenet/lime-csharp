using Lime.Protocol.Serialization.Newtonsoft.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;

namespace Lime.Protocol.Serialization.Newtonsoft
{
    public class JsonNetSerializer : IEnvelopeSerializer
    {
        static JsonNetSerializer()
        {
            JsonConvert.DefaultSettings = () => Settings;
            _serializer = global::Newtonsoft.Json.JsonSerializer.Create(JsonNetSerializer.Settings);
        }

        private static global::Newtonsoft.Json.JsonSerializerSettings _settings;
        private static global::Newtonsoft.Json.JsonSerializer _serializer;

        public static global::Newtonsoft.Json.JsonSerializerSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = new global::Newtonsoft.Json.JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize
                    };
                    _settings.Converters.Add(new StringEnumConverter { CamelCaseText = false });
                    _settings.Converters.Add(new IdentityJsonConverter());
                    _settings.Converters.Add(new NodeJsonConverter());
                    _settings.Converters.Add(new LimeUriJsonConverter());
                    _settings.Converters.Add(new MediaTypeJsonConverter());
                    _settings.Converters.Add(new SessionJsonConverter());
                    _settings.Converters.Add(new AuthenticationJsonConverter());
                    _settings.Converters.Add(new MessageJsonConverter());
                    _settings.Converters.Add(new CommandJsonConverter());
                    _settings.Converters.Add(new DocumentJsonConverter());
                    _settings.Converters.Add(new DocumentCollectionJsonConverter());
                    _settings.Converters.Add(new IsoDateTimeConverter
                    {
                        DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ",
                        DateTimeStyles = DateTimeStyles.AdjustToUniversal
                    });

                }

                return _settings;
            }
        }

        #region IEnvelopeSerializer Members

        /// <summary>
        /// Serialize an envelope
        /// to a string
        /// </summary>
        /// <param name = "envelope"></param>
        /// <returns></returns>
        public string Serialize(Envelope envelope)
        {
            return JsonConvert.SerializeObject(envelope, Formatting.None, Settings);
        }

        /// <summary>
        /// Deserialize an envelope
        /// from a string
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
            else if (jObject.Property("event") != null)
            {
                return jObject.ToObject<Notification>(_serializer);
            }
            else if (jObject.Property("method") != null)
            {
                return jObject.ToObject<Command>(_serializer);
            }
            else if (jObject.Property("state") != null)
            {
                return jObject.ToObject<Session>(_serializer);
            }
            else
            {
                throw new ArgumentException("JSON string is not a valid envelope");
            }
        }

        #endregion
    }
}