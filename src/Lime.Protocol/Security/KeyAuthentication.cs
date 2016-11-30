using System.Runtime.Serialization;

namespace Lime.Protocol.Security
{
    /// <summary>
    /// Defines a authentication scheme that uses a key for authentication.
    /// Should be used only with encrypted sessions.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class KeyAuthentication : Authentication
    {
        public const string KEY_KEY = "key";

        public KeyAuthentication()
            : base(AuthenticationScheme.Key)
        {

        }


        /// <summary>
        /// Base64 representation of the identity key.
        /// </summary>
        [DataMember(Name = KEY_KEY)]
        public string Key { get; set; }

        /// <summary>
        /// Set a plain key to a Base64 representation.
        /// </summary>
        /// <param name="key"></param>
        public void SetToBase64Key(string key)
        {
            Key = string.IsNullOrWhiteSpace(key) ? key : key.ToBase64();
        }

        /// <summary>
        /// Gets the plain key decoded from the Base64 representation.
        /// </summary>
        /// <returns></returns>
        public string GetFromBase64Key()
        {
            return string.IsNullOrWhiteSpace(Key) ? Key : Key.FromBase64();
        }
    }
}
