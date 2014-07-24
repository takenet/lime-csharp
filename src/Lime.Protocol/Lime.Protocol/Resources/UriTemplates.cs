using System;
using System.Collections.Generic;
using System.Text;

namespace Lime.Protocol.Resources
{
    /// <summary>
    /// Stores the Uri templates
    /// for the common protocol resources
    /// </summary>
    public static class UriTemplates
    {
        /// <summary>
        /// Template for the
        /// account resource
        /// </summary>
        public const string ACCOUNT = "/account";

        /// <summary>
        /// Template for the
        /// presence resource
        /// </summary>
        public const string PRESENCE = "/presence";

        /// <summary>
        /// Template for the
        /// contacts resource
        /// </summary>
        public const string CONTACTS = "/contacts";

        /// <summary>
        /// Template for a
        /// specific contact resource
        /// </summary>
        public const string CONTACT = "/contacts/{contactIdentity}";

        /// <summary>
        /// Template for the
        /// groups resource
        /// </summary>
        public const string GROUPS = "/groups";
        
        /// <summary>
        /// Template for a
        /// specific group resource
        /// </summary>
        public const string GROUP = "/groups/{groupIdentity}";

        /// <summary>
        /// Template for a
        /// specific group members
        /// resource
        /// </summary>
        public const string GROUP_MEMBERS = "/groups/{groupIdentity}/members";

        /// <summary>
        /// Template for a 
        /// specific group member
        /// resource
        /// </summary>
        public const string GROUP_MEMBER = "/groups/{groupIdentity}/members/{memberIdentity}";

        /// <summary>
        /// Template for the
        /// ping resource
        /// </summary>
        public const string PING = "/ping";

        /// <summary>
        /// Template for the
        /// receipt resource
        /// </summary>
        public const string RECEIPT = "/receipt";        
    }
}
