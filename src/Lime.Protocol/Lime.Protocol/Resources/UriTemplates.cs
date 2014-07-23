using System;
using System.Collections.Generic;
using System.Text;

namespace Lime.Protocol.Resources
{
    public static class UriTemplates
    {
        public const string ACCOUNT = "/account";

        public const string PRESENCE = "/presence";

        public const string CONTACTS = "/contacts";

        public const string CONTACT = "/contacts/{contactIdentity}";

        public const string GROUPS = "/groups";
        
        public const string GROUP = "/groups/{groupIdentity}";

        public const string GROUP_MEMBERS = "/groups/{groupIdentity}/members";

        public const string GROUP_MEMBER = "/groups/{groupIdentity}/members/{memberIdentity}";

        public const string RECEIPT = "/receipt";
    }
}
