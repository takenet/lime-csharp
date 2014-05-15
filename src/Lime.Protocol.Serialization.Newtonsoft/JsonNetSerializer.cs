using Lime.Protocol.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// <param name="envelope"></param>
        /// <returns></returns>
        public string Serialize(Envelope envelope)
        {
            return JsonConvert.SerializeObject(envelope, Formatting.None, Settings);
        }

        /// <summary>
        /// Deserialize an envelope
        /// from a string
        /// </summary>
        /// <param name="envelopeString"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">JSON string is not a valid envelope</exception>
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

        #region IdentityJsonConverter

        private class IdentityJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Identity);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var tokenValue = reader.Value.ToString();

                    return Identity.Parse(tokenValue);
                }
                else
                {
                    return null;
                }
            }

            public override void WriteJson(global::Newtonsoft.Json.JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                if (value != null)
                {
                    Identity identity = (Identity)value;
                    writer.WriteValue(identity.ToString());
                }
                else
                {
                    writer.WriteNull();
                }
            }
        } 

        #endregion

        #region NodeJsonConverter

        private class NodeJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Node);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var tokenValue = reader.Value.ToString();

                    return Node.Parse(tokenValue);
                }
                else
                {
                    return null;
                }
            }

            public override void WriteJson(global::Newtonsoft.Json.JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                if (value != null)
                {
                    Node identity = (Node)value;
                    writer.WriteValue(identity.ToString());
                }
                else
                {
                    writer.WriteNull();
                }
            }
        }

        #endregion

        #region NodeJsonConverter

        private class MediaTypeJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(MediaType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var tokenValue = reader.Value.ToString();
                    return MediaType.Parse(tokenValue);
                }
                else
                {
                    return null;
                }
            }

            public override void WriteJson(global::Newtonsoft.Json.JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                if (value != null)
                {
                    MediaType identity = (MediaType)value;
                    writer.WriteValue(identity.ToString());
                }
                else
                {
                    writer.WriteNull();
                }
            }
        }

        #endregion

        #region AuthenticationJsonConverter

        private class AuthenticationJsonConverter : JsonConverter
        {
            public override bool CanWrite
            {
                get { return false; }
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(Authentication).IsAssignableFrom(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                return null;
            }

            public override void WriteJson(global::Newtonsoft.Json.JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region SessionJsonConverter

        private class SessionJsonConverter : JsonConverter
        {
            public override bool CanWrite
            {
                get { return false; }
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Session);
            }

            /// <summary>
            /// Reads the JSON representation of the object.
            /// </summary>
            /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
            /// <param name="objectType">Type of the object.</param>
            /// <param name="existingValue">The existing value of object being read.</param>
            /// <param name="serializer">The calling serializer.</param>
            /// <returns>
            /// The object value.
            /// </returns>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                object target = null;
                if (reader.TokenType != JsonToken.Null)
                {
                    JObject jObject = JObject.Load(reader);

                    var session = new Session();
                    serializer.Populate(jObject.CreateReader(), session);

                    if (jObject["authentication"] != null &&
                        jObject["scheme"] != null)
                    {
                        var authenticationScheme = jObject["scheme"]
                            .ToObject<AuthenticationScheme>();

                        Type authenticationType;

                        if (TypeUtil.TryGetTypeForAuthenticationScheme(authenticationScheme, out authenticationType))
                        {
                            session.Authentication = (Authentication)Activator.CreateInstance(authenticationType);
                            serializer.Populate(jObject["authentication"].CreateReader(), session.Authentication);
                        }
                    }

                    target = session;
                }

                return target;
            }

            public override void WriteJson(global::Newtonsoft.Json.JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

        }

        #endregion

        #region MessageJsonConverter

        private class MessageJsonConverter : JsonConverter
        {
            public override bool CanWrite
            {
                get { return false; }
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Message);
            }

            /// <summary>
            /// Reads the JSON representation of the object.
            /// </summary>
            /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
            /// <param name="objectType">Type of the object.</param>
            /// <param name="existingValue">The existing value of object being read.</param>
            /// <param name="serializer">The calling serializer.</param>
            /// <returns>
            /// The object value.
            /// </returns>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                object target = null;
                if (reader.TokenType != JsonToken.Null)
                {
                    JObject jObject = JObject.Load(reader);

                    if (jObject["content"] != null &&
                        jObject["type"] != null)
                    {
                        var message = new Message(Guid.Empty);
                        serializer.Populate(jObject.CreateReader(), message);
                        
                        var contentMediaType = jObject["type"].ToObject<MediaType>();

                        Type documentType;

                        if (TypeUtil.TryGetTypeForMediaType(contentMediaType, out documentType))
                        {
                            message.Content = (Document)Activator.CreateInstance(documentType);
                            serializer.Populate(jObject["content"].CreateReader(), message.Content);
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

            public override void WriteJson(global::Newtonsoft.Json.JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region CommandJsonConverter

        private class CommandJsonConverter : JsonConverter
        {
            public override bool CanWrite
            {
                get { return false; }
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Command);
            }

            /// <summary>
            /// Reads the JSON representation of the object.
            /// </summary>
            /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
            /// <param name="objectType">Type of the object.</param>
            /// <param name="existingValue">The existing value of object being read.</param>
            /// <param name="serializer">The calling serializer.</param>
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

                    if (jObject[Command.RESOURCE_KEY] != null &&
                        jObject[Command.TYPE_KEY] != null)
                    {
                        var contentMediaType = jObject[Command.TYPE_KEY].ToObject<MediaType>();

                        Type documentType;

                        if (TypeUtil.TryGetTypeForMediaType(contentMediaType, out documentType))
                        {
                            command.Resource = (Document)Activator.CreateInstance(documentType);
                            serializer.Populate(jObject[Command.RESOURCE_KEY].CreateReader(), command.Resource);
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

        #endregion

        #region DocumentJsonConverter

        private class DocumentJsonConverter : JsonConverter
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
                return typeof(Document).IsAssignableFrom(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                // The serialization is made by the
                // container class (Message or Command)
                return null;
            }

            public override void WriteJson(global::Newtonsoft.Json.JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}