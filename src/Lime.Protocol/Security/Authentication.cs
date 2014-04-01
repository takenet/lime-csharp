using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Security
{
    /// <summary>
    /// Base class for the supported 
    /// authentication schemes
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public abstract class Authentication : IJsonSerializable, IJsonWritable
    {
        private AuthenticationScheme _scheme;

        public Authentication(AuthenticationScheme scheme)
        {
            _scheme = scheme;
        }

        public AuthenticationScheme GetAuthenticationScheme()
        {
            return _scheme;
        }


        #region IJsonWritable Members

        public abstract void WriteJson(IJsonWriter writer);

        #endregion

        #region IJsonSerializable Members

        public string ToJson()
        {
            using (var writer = new JsonWriter())
            {
                WriteJson(writer);
                return writer.ToString();
            }
        }

        #endregion

        public static Authentication FromJsonObject(JsonObject jsonObject, AuthenticationScheme scheme)
        {
            if (jsonObject == null)
            {
                throw new ArgumentNullException("authenticationDictionary");
            }

            Type authenticationType;
            if (!TypeUtil.TryGetTypeForAuthenticationScheme(scheme, out authenticationType))
            {
                throw new ArgumentException("Unknown authentication type");
            }

            var fromDictionaryDelegate = TypeUtil.GetFromJsonObjectDelegate(authenticationType);

            return (Authentication)fromDictionaryDelegate.DynamicInvoke(jsonObject);
        }
    }

    /// <summary>
    /// Defines the valid authentication schemes values.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum AuthenticationScheme
    {
        /// <summary>
        /// The server doesn't requires a client credential, 
        /// and provides a temporary identity to the node. 
        /// Some restriction may apply to guest sessions, 
        /// like the inability of sending some commands or 
        /// other nodes may want to block messages originated 
        /// by guest identities.
        /// </summary>
        [EnumMember(Value = "guest")]
        Guest,
        /// <summary>
        /// Username and password authentication.
        /// </summary>
        [EnumMember(Value = "plain")]
        Plain
    }
}
