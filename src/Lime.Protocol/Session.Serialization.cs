using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    public partial class Session : IJsonWritable
    {
        #region IJsonWritable Members

        /// <summary>
        /// Writes the JSON representation
        /// of the object to the writer
        /// </summary>
        /// <param name="writer"></param>
        public override void WriteJson(IJsonWriter writer)
        {
            base.WriteJson(writer);
            writer.WriteJsonProperty(AUTHENTICATION_KEY, this.Authentication);
            writer.WriteProperty(COMPRESSION_KEY, this.Compression, true);
            writer.WriteArrayProperty(COMPRESSION_OPTIONS_KEY, this.CompressionOptions, true);
            writer.WriteProperty(ENCRYPTION_KEY, this.Encryption, true);
            writer.WriteArrayProperty(ENCRYPTION_OPTIONS_KEY, this.EncryptionOptions, true);
            if (this.Mode != SessionMode.Node) writer.WriteProperty(MODE_KEY, this.Mode, true);
            writer.WriteJsonProperty(REASON_KEY, this.Reason);
            writer.WriteProperty(SCHEME_KEY, this.Scheme, true);
            writer.WriteArrayProperty(SCHEME_OPTIONS_KEY, this.SchemeOptions, true);
            writer.WriteProperty(STATE_KEY, this.State, true);
        }

        #endregion

        /// <summary>
        /// Creates an instance of the type
        /// using a dictionary to provide the
        /// field values
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        [Factory]
        public static Session FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                throw new ArgumentNullException("dictionary");
            }

            var session = new Session();
            Envelope.Populate(session, jsonObject);

            if (jsonObject.ContainsKey(SCHEME_KEY) &&
                jsonObject.ContainsKey(AUTHENTICATION_KEY))
            {
                var scheme = jsonObject.GetEnumValueOrDefault<AuthenticationScheme>(SCHEME_KEY);
                var authenticationJsonObject = (JsonObject)jsonObject[AUTHENTICATION_KEY];
                session.Authentication = Authentication.FromJsonObject(authenticationJsonObject, scheme);
            }

            session.Compression = jsonObject.GetEnumValueOrNull<SessionCompression>(COMPRESSION_KEY);
            session.CompressionOptions = jsonObject.GetEnumArrayOrNull<SessionCompression>(COMPRESSION_OPTIONS_KEY);
            session.Encryption = jsonObject.GetEnumValueOrNull<SessionEncryption>(ENCRYPTION_KEY);
            session.EncryptionOptions = jsonObject.GetEnumArrayOrNull<SessionEncryption>(ENCRYPTION_OPTIONS_KEY);
            session.Mode = jsonObject.GetEnumValueOrDefault<SessionMode>(MODE_KEY);

            if (jsonObject.ContainsKey(REASON_KEY))
            {
                session.Reason = Reason.FromJsonObject((JsonObject)jsonObject[REASON_KEY]);
            }

            session.SchemeOptions = jsonObject.GetEnumArrayOrNull<AuthenticationScheme>(SCHEME_OPTIONS_KEY);
            session.State = jsonObject.GetEnumValueOrDefault<SessionState>(STATE_KEY);


            return session;
        }

    }
}
