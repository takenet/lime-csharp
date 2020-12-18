using Lime.Messaging.Resources;
using Lime.Protocol;
using Lime.Protocol.Security;
using System;

namespace Lime.Cli
{
    public class ConnectionInformation
    {
        public Identity Identity { get; set; }

        public string Password { get; set; }
        
        public string Key { get; set; }

        /// <summary>
        /// Token to be used with External Authentication
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Issuer to be used with External Authentication. Defaults to accounts.blip.ai
        /// </summary>
        public string Issuer { get; set; }

        public string Instance { get; set; }

        public Uri ServerUri { get; set; }

        public Presence Presence { get; set; }

        public Receipt Receipt { get; set; }

        /// <summary>
        /// Thumbprint for the X509 Certificate
        /// </summary>
        public string Thumbprint { get; set; }

        /// <summary>
        /// Domain role to use as Transport Authentication
        /// </summary>
        public DomainRole DomainRole { get; set; }
    }
}
