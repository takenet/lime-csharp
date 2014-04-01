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
    /// Defines a plain authentication scheme,
    /// that uses a password for authentication.
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
        [DataMember(Name = "password")]
        public string Password { get; set; }
    }
}
