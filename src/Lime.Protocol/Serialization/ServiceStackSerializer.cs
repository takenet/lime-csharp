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
        }

        #endregion

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
            var jsonObject = ServiceStack.Text.JsonObject.Parse(envelopeString);

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

        private static Session DeserializeAsSession(ServiceStack.Text.JsonObject jsonObject)
        {
            var session = CreateEnvelope<Session>(jsonObject);
            session.Mode = jsonObject.Get<SessionMode>("mode");
            session.State = jsonObject.Get<SessionState>("state");
            session.Reason = jsonObject.Get<Reason>("reason");
            session.Encryption = jsonObject.Get<SessionEncryption?>("encryption");
            session.EncryptionOptions = jsonObject.Get<SessionEncryption[]>("encryptionOptions");
            session.Compression = jsonObject.Get<SessionCompression?>("compression");
            session.CompressionOptions = jsonObject.Get<SessionCompression[]>("compressionOptions");
            session.SchemeOptions = jsonObject.Get<AuthenticationScheme[]>("schemeOptions");

            if (jsonObject.ContainsKey("authentication"))
            {
                AuthenticationScheme scheme;
                if (!Enum.TryParse<AuthenticationScheme>(jsonObject["scheme"], true, out scheme))
                {
                    throw new ArgumentException("Invalid or unknown authentication scheme name");
                }

                Type authenticationType;
                if (!TypeUtil.TryGetTypeForAuthenticationScheme(scheme, out authenticationType))
                {
                    throw new ArgumentException("Unknown authentication mechanism");
                }

                session.Authentication = (Authentication)JsonSerializer.DeserializeFromString(
                    jsonObject.GetUnescaped("authentication"), authenticationType);
            }

            return session;
        }

        private static Message DeserializeAsMessage(ServiceStack.Text.JsonObject jsonObject)
        {
            var message = CreateEnvelope<Message>(jsonObject);
            message.Content = GetDocument(jsonObject, "content");
            return message;
        }

        private static Command DeserializeAsCommand(ServiceStack.Text.JsonObject jsonObject)
        {
            var command = CreateEnvelope<Command>(jsonObject);
            command.Method = jsonObject.Get<CommandMethod>("method");
            command.Reason = jsonObject.Get<Reason>("reason");
            command.Status = jsonObject.Get<CommandStatus>("status");

            if (jsonObject.ContainsKey("resource"))
            {
                command.Resource = GetDocument(jsonObject, "resource");
            }

            return command;
        }

        private static Document GetDocument(ServiceStack.Text.JsonObject jsonObject, string documentPropertyName)
        {
            if (!jsonObject.ContainsKey("type"))
            {
                throw new ArgumentException("Type information not found");
            }

            var mediaType = jsonObject.Get<MediaType>("type");

            Type documentType;

            if (!TypeUtil.TryGetTypeForMediaType(mediaType, out documentType))
            {
                throw new ArgumentException("Unknown document type");
            }

            return (Document)JsonSerializer.DeserializeFromString(
                jsonObject.GetUnescaped(documentPropertyName), documentType);
        }

        private static TEnvelope CreateEnvelope<TEnvelope>(ServiceStack.Text.JsonObject j) where TEnvelope : Envelope, new()
        {
            return new TEnvelope()
            {
                Id = j.Get<Guid>("id"),
                From = j.Get<Node>("from"),
                Pp = j.Get<Node>("pp"),
                To = j.Get<Node>("to"),
                Metadata = j.Get<Dictionary<string, string>>("metadata")
            };
        }

        #endregion
    }
}
