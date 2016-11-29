using System;
using System.Runtime.Serialization;
using System.Security;
using Lime.Protocol.Serialization;

namespace Lime.Protocol.Security
{
    /// <summary>
    /// Defines a plain authentication scheme, that uses a password for authentication.
    /// Should be used only with encrypted sessions.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class PlainAuthentication : Authentication
    {
        public const string PASSWORD_KEY = "password";

        public PlainAuthentication()
            : base(AuthenticationScheme.Plain)
        {

        }

        /// <summary>
        /// Base64 representation of the 
        /// identity password
        /// </summary>
        [DataMember(Name = PASSWORD_KEY)]
        public string Password { get; set; }

        /// <summary>
        /// Set a plain password to a 
        /// Base64 representation
        /// </summary>
        /// <param name="password"></param>
        public void SetToBase64Password(string password)
        {
            Password = string.IsNullOrWhiteSpace(password) ? password : password.ToBase64();
        }

        /// <summary>
        /// Gets the plain password decoded 
        /// from the Base64 representation
        /// </summary>
        /// <returns></returns>
        public string GetFromBase64Password()
        {
            return string.IsNullOrWhiteSpace(Password) ? Password : Password.FromBase64();
        }
    }
}
