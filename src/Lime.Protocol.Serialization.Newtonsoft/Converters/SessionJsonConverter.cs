using Lime.Protocol.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Lime.Protocol.Serialization.Newtonsoft.Converters
{
    class SessionJsonConverter : JsonConverter
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
            return objectType == typeof (Session);
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
                var session = new Session();
                serializer.Populate(jObject.CreateReader(), session);
                if (jObject[Session.SCHEME_KEY] != null)
                {
                    var authenticationScheme = jObject[Session.SCHEME_KEY].ToObject<AuthenticationScheme>();
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
}