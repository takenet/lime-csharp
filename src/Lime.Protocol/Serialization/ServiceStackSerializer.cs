using Lime.Protocol.Security;
using ServiceStack;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Implements JSON serialization using 
    /// the ServiceStack.Text library
    /// </summary>
    public class ServiceStackSerializer : IEnvelopeSerializer
    {
        #region Constructors

        static ServiceStackSerializer()
        {
            JsConfig.ExcludeTypeInfo = true;
            JsConfig.EmitCamelCaseNames = true;
            

            JsConfig<Message>.IncludeTypeInfo = false;
            JsConfig<Notification>.IncludeTypeInfo = false;
            JsConfig<Command>.IncludeTypeInfo = false;
            JsConfig<Session>.IncludeTypeInfo = false;

            JsConfig<MediaType>.SerializeFn = m => m.ToString();
            JsConfig<MediaType>.DeSerializeFn = s => MediaType.Parse(s);
            JsConfig<Node>.SerializeFn = n => n.ToString();
            JsConfig<Node>.DeSerializeFn = s => Node.Parse(s);
            JsConfig<Identity>.SerializeFn = i => i.ToString();
            JsConfig<Identity>.DeSerializeFn = s => Identity.Parse(s);
            JsConfig<Guid?>.SerializeFn = g => { if (g.HasValue) return g.ToString(); else return null; };
            JsConfig<Guid?>.DeSerializeFn = s => { if (string.IsNullOrWhiteSpace(s)) return null; else return new Guid(s); };                      

            foreach (var enumType in TypeUtil.GetEnumTypes())
            {
                var jsonConfigEnumType = typeof(JsConfig<>).MakeGenericType(enumType);
                var serializeProperty = jsonConfigEnumType.GetProperty("SerializeFn");
                var serializeFuncType = typeof(Func<,>).MakeGenericType(enumType, typeof(string));
                var methodInfo = typeof(ServiceStackSerializer).GetMethod("ToCamelCase", BindingFlags.Static | BindingFlags.NonPublic);
                var enumToCamelCaseMethod = methodInfo.MakeGenericMethod(enumType);
                var serializeFunc = Delegate.CreateDelegate(serializeFuncType, enumToCamelCaseMethod);
                serializeProperty.SetValue(null, serializeFunc, BindingFlags.Static, null, null, CultureInfo.InvariantCulture);
            }
        }

        #endregion


        private static string ToCamelCase<T>(T value)
        {
            if (value == null)
            {
                return null;
            }
            return value.ToString().ToCamelCase();
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
            return JsonSerializer.SerializeToString(envelope);
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
            var jsonObject = JsonObject.Parse(envelopeString);

            if (jsonObject.ContainsKey("content"))
            {
                return DeserializeAsMessage(jsonObject);
            }
            else if (jsonObject.ContainsKey("event"))
            {
                return JsonSerializer.DeserializeFromString<Notification>(envelopeString);
            }
            else if (jsonObject.ContainsKey("method"))
            {
                return DeserializeAsCommand(jsonObject);
            }
            else if (jsonObject.ContainsKey("state"))
            {
                return DeserializeAsSession(jsonObject);
            }
            else
            {
                throw new ArgumentException("JSON string is not a valid envelope");
            }
        }

        #endregion

        #region Private methods

        private static Session DeserializeAsSession(JsonObject jsonObject)
        {
            var session = CreateEnvelope<Session>(jsonObject);
            session.Mode = jsonObject.Get<SessionMode>(Session.MODE_KEY);
            session.State = jsonObject.Get<SessionState>(Session.STATE_KEY);
            session.Reason = jsonObject.Get<Reason>(Session.REASON_KEY);
            session.Encryption = jsonObject.Get<SessionEncryption?>(Session.ENCRYPTION_KEY);
            session.EncryptionOptions = jsonObject.Get<SessionEncryption[]>(Session.ENCRYPTION_OPTIONS_KEY);
            session.Compression = jsonObject.Get<SessionCompression?>(Session.COMPRESSION_KEY);
            session.CompressionOptions = jsonObject.Get<SessionCompression[]>(Session.COMPRESSION_OPTIONS_KEY);
            session.SchemeOptions = jsonObject.Get<AuthenticationScheme[]>(Session.SCHEME_OPTIONS_KEY);

            if (jsonObject.ContainsKey(Session.AUTHENTICATION_KEY))
            {
                AuthenticationScheme scheme;
                if (!Enum.TryParse<AuthenticationScheme>(jsonObject[Session.SCHEME_KEY], true, out scheme))
                {
                    throw new ArgumentException("Invalid or unknown authentication scheme name");
                }

                Type authenticationType;
                if (!TypeUtil.TryGetTypeForAuthenticationScheme(scheme, out authenticationType))
                {
                    throw new ArgumentException("Unknown authentication mechanism");
                }

                session.Authentication = (Authentication)JsonSerializer.DeserializeFromString(
                    jsonObject.GetUnescaped(Session.AUTHENTICATION_KEY), authenticationType);
            }

            return session;
        }

        private static Message DeserializeAsMessage(JsonObject jsonObject)
        {
            var message = CreateEnvelope<Message>(jsonObject);
            message.Content = GetDocument(jsonObject, Message.TYPE_KEY, Message.CONTENT_KEY);
            return message;
        }

        private static Command DeserializeAsCommand(JsonObject jsonObject)
        {
            var command = CreateEnvelope<Command>(jsonObject);
            command.Method = jsonObject.Get<CommandMethod>(Command.METHOD_KEY);
            command.Reason = jsonObject.Get<Reason>(Command.REASON_KEY);
            command.Status = jsonObject.Get<CommandStatus>(Command.STATUS_KEY);
            command.Resource = GetDocument(jsonObject, Command.TYPE_KEY, Command.RESOURCE_KEY);

            return command;
        }

        private static TEnvelope CreateEnvelope<TEnvelope>(JsonObject j) where TEnvelope : Envelope, new()
        {
            return new TEnvelope()
            {
                Id = j.Get<Guid?>(Envelope.ID_KEY),
                From = j.Get<Node>(Envelope.FROM_KEY),
                Pp = j.Get<Node>(Envelope.PP_KEY),
                To = j.Get<Node>(Envelope.TO_KEY),
                Metadata = j.Get<Dictionary<string, string>>(Envelope.METADATA_KEY)
            };
        }

        private static Document GetDocument(JsonObject jsonObject, string typeKey, string documentPropertyName)
        {
            Document document = null;

            if (jsonObject.ContainsKey(typeKey))
            {
                var mediaType = jsonObject.Get<MediaType>(typeKey);

                Type documentType;

                if (!TypeUtil.TryGetTypeForMediaType(mediaType, out documentType))
                {
                    throw new ArgumentException("Unknown document type");
                }

                document = (Document)JsonSerializer.DeserializeFromString(
                    jsonObject.GetUnescaped(documentPropertyName), documentType);
            }

            return document;
        }

        #endregion
    }
}


