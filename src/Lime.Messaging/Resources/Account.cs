using System.Runtime.Serialization;
using System.Security;
using Lime.Protocol;
using System;

namespace Lime.Messaging.Resources
{
    /// <summary>
    /// Represents an user account information.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Account : ContactDocument
    {
        public const string RESOURCE_PATH = "/account";

        public const string MIME_TYPE = "application/vnd.lime.account+json";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        public const string FULL_NAME_KEY = "fullName"; 
        public const string IS_TEMPORARY_KEY = "isTemporary";
        public const string PASSWORD_KEY = "password";
        public const string OLD_PASSWORD_KEY = "oldPassword";
        public const string INBOX_SIZE_KEY = "inboxSize";
        public const string ALLOW_ANONYMOUS_SENDER_KEY = "allowAnonymousSender";
        public const string ALLOW_UNKNOWN_SENDER_KEY = "allowUnknownSender";
        public const string STORE_MESSAGE_CONTENT_KEY = "storeMessageContent";
        public const string ACCESS_KEY_KEY = "accessKey";
        public const string ALTERNATIVE_ACCOUNT_KEY = "alternativeAccount";
        public const string PUBLISH_TO_DIRECTORY_KEY = "publishToDirectory";


        /// <summary>
        /// Initializes a new instance of the <see cref="Account"/> class.
        /// </summary>
        public Account()
            : base(MediaType)
        {

        }

        /// <summary>
        /// The user full name.
        /// </summary>
        [DataMember(Name = FULL_NAME_KEY)]
        public string FullName { get; set; }        

        /// <summary>
        /// Indicates that the account is temporary is valid only in the current session.
        /// </summary>
        [DataMember(Name = IS_TEMPORARY_KEY)]
        public bool? IsTemporary { get; set; }

        /// <summary>
        /// Base64 representation of the account password.
        /// </summary>
        [DataMember(Name = PASSWORD_KEY)]
        public string Password { get; set; }

        /// <summary>
        /// Base64 representation of the account password. 
        /// Mandatory in case of updating account password.
        /// </summary>
        [DataMember(Name = OLD_PASSWORD_KEY)]
        public string OldPassword { get; set; }

        /// <summary>
        /// Access key for updating the account without knowing the old password.
        /// </summary>
        [DataMember(Name = ACCESS_KEY_KEY)]
        public string AccessKey { get; set; }

        /// <summary>
        /// Size of account inbox for storing offline messages.
        /// </summary>
        [DataMember(Name = INBOX_SIZE_KEY)]
        public int? InboxSize { get; set; }

        /// <summary>
        /// Indicates if this account allows receive messages from anonymous users.
        /// </summary>
        [DataMember(Name = ALLOW_ANONYMOUS_SENDER_KEY)]
        public bool? AllowAnonymousSender { get; set; }

        /// <summary>
        /// Indicates if this account allows receive messages from users that are not in the account contact list.
        /// </summary>
        [DataMember(Name = ALLOW_UNKNOWN_SENDER_KEY)]
        public bool? AllowUnknownSender { get; set; }

        /// <summary>
        /// Indicates if the content of messages from this account should be stored in the server. 
        /// Note that for offline messages, this will always happens.
        /// </summary>
        [DataMember(Name = STORE_MESSAGE_CONTENT_KEY, EmitDefaultValue = false)]
        public bool? StoreMessageContent { get; set; }

        /// <summary>
        /// Alternative account address.
        /// </summary>
        [DataMember(Name = ALTERNATIVE_ACCOUNT_KEY)]
        public Identity AlternativeAccount { get; set; }

        /// <summary>
        /// Indicates if the account info should be published to the domain directory.
        /// </summary>
        [DataMember(Name = PUBLISH_TO_DIRECTORY_KEY)]
        public bool? PublishToDirectory { get; set; }
    }
}