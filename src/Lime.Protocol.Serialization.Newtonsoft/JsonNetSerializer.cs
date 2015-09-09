using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using Lime.Protocol.Serialization.Newtonsoft.Converters;

namespace Lime.Protocol.Serialization.Newtonsoft
{
    public class JsonNetSerializer : IEnvelopeSerializer
    {
        static JsonNetSerializer()
        {
            JsonConvert.DefaultSettings = () => JsonNetSerializer.Settings;
        }

        private static global::Newtonsoft.Json.JsonSerializerSettings _settings;
        public static global::Newtonsoft.Json.JsonSerializerSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = new global::Newtonsoft.Json.JsonSerializerSettings();
                    _settings.NullValueHandling = NullValueHandling.Ignore;
                    _settings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                    _settings.Converters.Add(new StringEnumConverter());
                    _settings.Converters.Add(new IdentityJsonConverter());
                    _settings.Converters.Add(new NodeJsonConverter());
                    _settings.Converters.Add(new LimeUriJsonConverter());
                    _settings.Converters.Add(new MediaTypeJsonConverter());
                    _settings.Converters.Add(new SessionJsonConverter());
                    _settings.Converters.Add(new AuthenticationJsonConverter());
                    _settings.Converters.Add(new MessageJsonConverter());
                    _settings.Converters.Add(new CommandJsonConverter());
                    _settings.Converters.Add(new DocumentJsonConverter());
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
            var jsonObject = (JObject)JsonConvert.DeserializeObject(envelopeString, Settings);
            if (jsonObject.Property("content") != null)
            {
                return jsonObject.ToObject<Message>();
            }
            else if (jsonObject.Property("event") != null)
            {
                return jsonObject.ToObject<Notification>();
            }
            else if (jsonObject.Property("method") != null)
            {
                return jsonObject.ToObject<Command>();
            }
            else if (jsonObject.Property("state") != null)
            {
                return jsonObject.ToObject<Session>();
            }
            else
            {
                throw new ArgumentException("JSON string is not a valid envelope");
            }
        }

        #endregion
    }
}