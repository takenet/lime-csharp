using Lime.Protocol.Security;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lime.Protocol.Serialization.SystemTextJson.Converters
{
    /// <summary>
    /// A <see cref="JsonConverter{T}"/> for <see cref="Session"/> that handles the
    /// polymorphic <see cref="Authentication"/> property based on the <c>scheme</c> value.
    /// </summary>
    public class SessionJsonConverter : JsonConverter<Session>
    {
        public override Session Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            using var doc = System.Text.Json.JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            // Deserialize the session with all basic properties first
            var session = new Session();
            PopulateSessionProperties(session, root, options);

            // Handle polymorphic authentication
            if (root.TryGetProperty(Session.SCHEME_KEY, out var schemeElement) &&
                schemeElement.ValueKind != JsonValueKind.Null)
            {
                var scheme = JsonSerializer.Deserialize<AuthenticationScheme>(schemeElement.GetRawText(), options);
                if (TypeUtil.TryGetTypeForAuthenticationScheme(scheme, out var authenticationType))
                {
                    var authentication = (Authentication)Activator.CreateInstance(authenticationType);
                    if (root.TryGetProperty(Session.AUTHENTICATION_KEY, out var authElement) &&
                        authElement.ValueKind == JsonValueKind.Object)
                    {
                        authentication = (Authentication)JsonSerializer.Deserialize(
                            authElement.GetRawText(), authenticationType, options);
                    }
                    session.Authentication = authentication;
                }
            }

            return session;
        }

        private static void PopulateSessionProperties(Session session, JsonElement root, JsonSerializerOptions options)
        {
            if (root.TryGetProperty(Envelope.ID_KEY, out var idElement) &&
                idElement.ValueKind != JsonValueKind.Null)
            {
                session.Id = idElement.GetString();
            }

            if (root.TryGetProperty(Envelope.FROM_KEY, out var fromElement) &&
                fromElement.ValueKind != JsonValueKind.Null)
            {
                session.From = JsonSerializer.Deserialize<Node>(fromElement.GetRawText(), options);
            }

            if (root.TryGetProperty(Envelope.TO_KEY, out var toElement) &&
                toElement.ValueKind != JsonValueKind.Null)
            {
                session.To = JsonSerializer.Deserialize<Node>(toElement.GetRawText(), options);
            }

            if (root.TryGetProperty(Envelope.PP_KEY, out var ppElement) &&
                ppElement.ValueKind != JsonValueKind.Null)
            {
                session.Pp = JsonSerializer.Deserialize<Node>(ppElement.GetRawText(), options);
            }

            if (root.TryGetProperty(Envelope.METADATA_KEY, out var metadataElement) &&
                metadataElement.ValueKind == JsonValueKind.Object)
            {
                session.Metadata = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(
                    metadataElement.GetRawText(), options);
            }

            if (root.TryGetProperty(Session.STATE_KEY, out var stateElement) &&
                stateElement.ValueKind != JsonValueKind.Null)
            {
                session.State = JsonSerializer.Deserialize<SessionState>(stateElement.GetRawText(), options);
            }

            if (root.TryGetProperty(Session.ENCRYPTION_OPTIONS_KEY, out var encOptsElement) &&
                encOptsElement.ValueKind == JsonValueKind.Array)
            {
                session.EncryptionOptions = JsonSerializer.Deserialize<SessionEncryption[]>(encOptsElement.GetRawText(), options);
            }

            if (root.TryGetProperty(Session.ENCRYPTION_KEY, out var encElement) &&
                encElement.ValueKind != JsonValueKind.Null)
            {
                session.Encryption = JsonSerializer.Deserialize<SessionEncryption?>(encElement.GetRawText(), options);
            }

            if (root.TryGetProperty(Session.COMPRESSION_OPTIONS_KEY, out var compOptsElement) &&
                compOptsElement.ValueKind == JsonValueKind.Array)
            {
                session.CompressionOptions = JsonSerializer.Deserialize<SessionCompression[]>(compOptsElement.GetRawText(), options);
            }

            if (root.TryGetProperty(Session.COMPRESSION_KEY, out var compElement) &&
                compElement.ValueKind != JsonValueKind.Null)
            {
                session.Compression = JsonSerializer.Deserialize<SessionCompression?>(compElement.GetRawText(), options);
            }

            if (root.TryGetProperty(Session.SCHEME_OPTIONS_KEY, out var schemeOptsElement) &&
                schemeOptsElement.ValueKind == JsonValueKind.Array)
            {
                session.SchemeOptions = JsonSerializer.Deserialize<AuthenticationScheme[]>(schemeOptsElement.GetRawText(), options);
            }

            if (root.TryGetProperty(Session.REASON_KEY, out var reasonElement) &&
                reasonElement.ValueKind == JsonValueKind.Object)
            {
                session.Reason = JsonSerializer.Deserialize<Reason>(reasonElement.GetRawText(), options);
            }
        }

        public override void Write(Utf8JsonWriter writer, Session value, JsonSerializerOptions options)
        {
            // Use standard serialization (no special write handling needed)
            writer.WriteStartObject();

            WriteStringPropertyIfNotNull(writer, Envelope.ID_KEY, value.Id);
            WriteObjectPropertyIfNotNull(writer, Envelope.FROM_KEY, value.From, options);
            WriteObjectPropertyIfNotNull(writer, Envelope.TO_KEY, value.To, options);
            WriteObjectPropertyIfNotNull(writer, Envelope.PP_KEY, value.Pp, options);

            if (value.Metadata != null)
            {
                writer.WritePropertyName(Envelope.METADATA_KEY);
                JsonSerializer.Serialize(writer, value.Metadata, options);
            }

            writer.WritePropertyName(Session.STATE_KEY);
            JsonSerializer.Serialize(writer, value.State, options);

            if (value.EncryptionOptions != null)
            {
                writer.WritePropertyName(Session.ENCRYPTION_OPTIONS_KEY);
                JsonSerializer.Serialize(writer, value.EncryptionOptions, options);
            }

            if (value.Encryption.HasValue)
            {
                writer.WritePropertyName(Session.ENCRYPTION_KEY);
                JsonSerializer.Serialize(writer, value.Encryption, options);
            }

            if (value.CompressionOptions != null)
            {
                writer.WritePropertyName(Session.COMPRESSION_OPTIONS_KEY);
                JsonSerializer.Serialize(writer, value.CompressionOptions, options);
            }

            if (value.Compression.HasValue)
            {
                writer.WritePropertyName(Session.COMPRESSION_KEY);
                JsonSerializer.Serialize(writer, value.Compression, options);
            }

            if (value.SchemeOptions != null)
            {
                writer.WritePropertyName(Session.SCHEME_OPTIONS_KEY);
                JsonSerializer.Serialize(writer, value.SchemeOptions, options);
            }

            if (value.Authentication != null)
            {
                var scheme = value.Authentication.GetAuthenticationScheme();
                writer.WritePropertyName(Session.SCHEME_KEY);
                JsonSerializer.Serialize(writer, scheme, options);

                writer.WritePropertyName(Session.AUTHENTICATION_KEY);
                JsonSerializer.Serialize(writer, value.Authentication, value.Authentication.GetType(), options);
            }

            WriteObjectPropertyIfNotNull(writer, Session.REASON_KEY, value.Reason, options);

            writer.WriteEndObject();
        }

        private static void WriteStringPropertyIfNotNull(Utf8JsonWriter writer, string name, string value)
        {
            if (value != null)
            {
                writer.WriteString(name, value);
            }
        }

        private static void WriteObjectPropertyIfNotNull<TValue>(
            Utf8JsonWriter writer, string name, TValue value, JsonSerializerOptions options)
        {
            if (value != null)
            {
                writer.WritePropertyName(name);
                JsonSerializer.Serialize(writer, value, options);
            }
        }
    }
}
