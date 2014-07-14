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
                    _settings.Converters.Add(new LimeUriConverter());
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

        #region LimeUriConverter

        private class LimeUriConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(LimeUri);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var tokenValue = reader.Value.ToString();

                    return LimeUri.Parse(tokenValue);
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
                    LimeUri identity = (LimeUri)value;
                    writer.WriteValue(identity.ToString());
                }
                else
                {
                    writer.WriteNull();
                }
            }
        }

        #endregion

        #region MediaTypeJsonConverter

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

                    if (jObject[Session.SCHEME_KEY] != null)
                    {
                        var authenticationScheme = jObject[Session.SCHEME_KEY]
                            .ToObject<AuthenticationScheme>();

                        Type authenticationType;

                        if (TypeUtil.TryGetTypeForAuthenticationScheme(authenticationScheme, out authenticationType))
                        {
                            session.Authentication = (Authentication)Activator.CreateInstance(authenticationType);
                            if (jObject[Session.AUTHENTICATION_KEY] != null)
                            {
                                serializer.Populate(jObject[Session.AUTHENTICATION_KEY].CreateReader(), session.Authentication);
                            }
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
                get { return true; }
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

                    if (jObject[Message.CONTENT_KEY] != null &&
                        jObject[Message.TYPE_KEY] != null)
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
                                    var contentDictionary = contentJsonObject.ToDictionary(k => k.Key, v => (object)v.Value.ToString());
                                    message.Content = new JsonDocument(contentDictionary, contentMediaType);                                        
                                }
                                else
                                {
                                    throw new ArgumentException("The property is not a JSON");
                                }                                    
                            }
                            else
                            {
                                message.Content = new PlainDocument(
                                    jObject[Message.CONTENT_KEY].ToString(),
                                    contentMediaType);
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

            public override void WriteJson(global::Newtonsoft.Json.JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
            {                                
                var message = (Message)value;               

                if (message.Type.IsJson)
                {
                    if (message.Content is JsonDocument)
                    {
                        throw new NotSupportedException("The content type is not supported by this serializer");
                    }

                    serializer.Serialize(writer, value);
                }
                else
                {
                    writer.WriteStartObject();

                    writer.WriteValueIfNotDefault(Envelope.ID_KEY, message.Id);
                    writer.WriteValueIfNotDefaultAsString(Envelope.FROM_KEY, message.From);
                    writer.WriteValueIfNotDefaultAsString(Envelope.TO_KEY, message.To);
                    writer.WriteValueIfNotDefaultAsString(Envelope.PP_KEY, message.Pp);

                    writer.WritePropertyName(Message.TYPE_KEY);
                    writer.WriteValue(message.Type.ToString());
                    writer.WritePropertyName(Message.CONTENT_KEY);                    
                    writer.WriteValue(message.Content.ToString());
                    
                    if (message.Metadata != null)
                    {
                        writer.WritePropertyName(Message.METADATA_KEY);
                        writer.WriteStartObject();

                        foreach (var item in message.Metadata)
                        {
                            writer.WritePropertyName(item.Key);
                            writer.WriteValue(item.Value);
                        }

                        writer.WriteEndObject();
                    }                    
                }
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
                                    serializer.Populate(jObject[Command.RESOURCE_KEY].CreateReader(), command.Resource);
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
                                command.Resource = new PlainDocument(
                                    jObject[Command.RESOURCE_KEY].ToString(),
                                    resourceMediaType);
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

    public static class JsonWriterExtensions
    {
        public static void WriteValueIfNotDefault<T>(this JsonWriter writer, string propertyName, T value)
        {
            if (value != null && 
                !value.Equals(default(T)))
            {
                writer.WritePropertyName(propertyName);
                writer.WriteValue(value);
            }
        }

        public static void WriteValueIfNotDefaultAsString<T>(this JsonWriter writer, string propertyName, T value)
        {
            if (value != null &&
                !value.Equals(default(T)))
            {
                writer.WritePropertyName(propertyName);
                writer.WriteValue(value.ToString());
            }
        }

    }
}