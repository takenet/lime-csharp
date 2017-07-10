using System;
using System.Runtime.Serialization;

namespace Lime.Protocol.Security
{
    /// <summary>
    /// Defines a external authentication scheme, that uses third-party validation.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class ExternalAuthentication : Authentication
    {
        public const string TOKEN_KEY = "token";
        public const string ISSUER_KEY = "issuer";

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalAuthentication"/> class.
        /// </summary>
        public ExternalAuthentication() 
            : base(AuthenticationScheme.External)
        {
        }

        /// <summary>
        /// Gets or sets the authentication token on base64 representation.
        /// </summary>
        [DataMember(Name = TOKEN_KEY)]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the trusted token issuer.
        /// </summary>
        [DataMember(Name = ISSUER_KEY)]
        public string Issuer { get; set; }

        /// <summary>
        /// Set a plain token to a base64 representation
        /// </summary>
        /// <param name="password"></param>
        public void SetToBase64Token(string password)
        {
            Token = string.IsNullOrWhiteSpace(password) ? password : password.ToBase64();
        }

        /// <summary>
        /// Gets the token decoded from the Base64 representation
        /// </summary>
        /// <returns></returns>
        public string GetFromBase64Token()
        {
            return string.IsNullOrWhiteSpace(Token) ? Token : Token.FromBase64();
        }
    }
}
