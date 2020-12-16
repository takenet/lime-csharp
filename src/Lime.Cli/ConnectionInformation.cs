using Lime.Messaging.Resources;
using Lime.Protocol;
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

        public string Instance { get; set; }

        public Uri ServerUri { get; set; }

        public Presence Presence { get; set; }

        public Receipt Receipt { get; set; }
    }
}
