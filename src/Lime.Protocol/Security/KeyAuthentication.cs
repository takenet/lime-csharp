using System;
using System.Runtime.Serialization;
using System.Security;
using Lime.Protocol.Serialization;

namespace Lime.Protocol.Security
{
    /// <summary>
    /// Defines a authentication scheme that uses a key for authentication.
    /// Should be used only with encrypted sessions.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class KeyAuthentication : Authentication, IDisposable
    {
        public const string KEY_KEY = "key";

        public KeyAuthentication()
            : base(AuthenticationScheme.Key)
        {

        }

        ~KeyAuthentication()
        {
            Dispose(false);
        }

        [IgnoreDataMember]
        public SecureString SecureKey { get; private set; }

        /// <summary>
        /// Base64 representation of the identity key.
        /// </summary>
        [DataMember(Name = KEY_KEY)]
        public string Key
        {
            get
            {
                return SecureKey?.ToUnsecureString();
            }
            set
            {
                if (SecureKey != null)
                {
                    SecureKey.Dispose();
                    SecureKey = null;
                }

                if (value != null)
                {
                    SecureKey = value.ToSecureString();
                }
            }
        }

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

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                SecureKey?.Dispose();
            }
        }

        #endregion 

    }
}
