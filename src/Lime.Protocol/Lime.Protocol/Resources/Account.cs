using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Resources
{
    /// <summary>
    /// Represents an user account information.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public partial class Account : Document
    {
        public const string RESOURCE_PATH = "/account";

        public const string MIME_TYPE = "application/vnd.lime.account+json";

        public const string FULL_NAME_KEY = "fullName";
        public const string ADDRESS_KEY = "address";
        public const string CITY_KEY = "city";
        public const string EMAIL_KEY = "email";
        public const string PHONE_NUMBER_KEY = "phoneNumber";
        public const string CELL_PHONE_NUMBER_KEY = "cellPhoneNumber";
        public const string IS_TEMPORARY_KEY = "isTemporary";
        public const string PASSWORD_KEY = "password";
        public const string OLD_PASSWORD_KEY = "oldPassword";
        public const string INBOX_SIZE_KEY = "inboxSize";
        public const string ALLOW_ANONYMOUS_SENDER_KEY = "allowAnonymousSender";
        public const string ALLOW_UNKNOWN_SENDER_KEY = "allowUnknownSender";
        public const string STORE_MESSAGE_CONTENT_KEY = "storeMessageContent";

        public Account()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }

        /// <summary>
        /// The user full name
        /// </summary>
        [DataMember(Name = FULL_NAME_KEY)]
        public string FullName { get; set; }

        /// <summary>
        ///  The user address
        /// </summary>
        [DataMember(Name = ADDRESS_KEY)]
        public string Address { get; set; }

        /// <summary>
        /// The user city
        /// </summary>
        [DataMember(Name = CITY_KEY)]
        public string City { get; set; }

        /// <summary>
        /// The user e-mail address
        /// </summary>
        [DataMember(Name = EMAIL_KEY)]
        public string Email { get; set; }

        /// <summary>
        /// The user phone number
        /// </summary>
        [DataMember(Name = PHONE_NUMBER_KEY)]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// The user cellphone number
        /// </summary>
        [DataMember(Name = CELL_PHONE_NUMBER_KEY)]
        public string CellPhoneNumber { get; set; }

        /// <summary>
        /// Indicates that the account is
        /// temporary is valid only in
        /// the current session
        /// </summary>
        [DataMember(Name = IS_TEMPORARY_KEY)]
        public bool? IsTemporary { get; set; }

#if !PCL
        [IgnoreDataMember]
        public SecureString SecurePassword { get; private set; }

        /// <summary>
        /// Base64 representation of the 
        /// account password
        /// </summary>
        [DataMember(Name = PASSWORD_KEY)]
        public string Password
        {
            get
            {
                if (SecurePassword != null)
                {
                    return SecurePassword.ToUnsecureString();
                }
                return null;
            }
            set
            {
                if (SecurePassword != null)
                {
                    SecurePassword.Dispose();
                    SecurePassword = null;
                }

                if (value != null)
                {
                    SecurePassword = value.ToSecureString();
                }
            }
        }

        [IgnoreDataMember]
        public SecureString SecureOldPassword { get; private set; }

        /// <summary>
        /// Base64 representation of the 
        /// account password. Mandatory
        /// in case of updating account
        /// password.
        /// </summary>
        [DataMember(Name = OLD_PASSWORD_KEY)]
        public string OldPassword
        {
            get
            {
                if (SecureOldPassword != null)
                {
                    return SecureOldPassword.ToUnsecureString();
                }
                return null;
            }
            set
            {
                if (SecureOldPassword != null)
                {
                    SecureOldPassword.Dispose();
                    SecureOldPassword = null;
                }

                if (value != null)
                {
                    SecureOldPassword = value.ToSecureString();
                }
            }
        }  
#else
        /// <summary>
        /// Base64 representation of the 
        /// account password
        /// </summary>
        [DataMember(Name = PASSWORD_KEY)]
        public string Password { get; set; }

        /// <summary>
        /// Base64 representation of the 
        /// account password. Mandatory
        /// in case of updating account
        /// password.
        /// </summary>
        [DataMember(Name = OLD_PASSWORD_KEY)]
        public string OldPassword { get; set; }
#endif

        /// <summary>
        /// Size of account inbox
        /// for storing offline messages
        /// </summary>
        [DataMember(Name = INBOX_SIZE_KEY)]
        public int? InboxSize { get; set; }

        /// <summary>
        /// Indicates if this account
        /// allows receive messages from
        /// anonymous users
        /// </summary>
        [DataMember(Name = ALLOW_ANONYMOUS_SENDER_KEY)]
        public bool? AllowAnonymousSender { get; set; }

        /// <summary>
        /// Indicates if this account
        /// allows receive messages from
        /// users that are not in
        /// the account contact list
        /// </summary>
        [DataMember(Name = ALLOW_UNKNOWN_SENDER_KEY)]
        public bool? AllowUnknownSender { get; set; }

        /// <summary>
        /// Indicates if the content of messages 
        /// from this account should be stored in 
        /// the server. Note that for offline messages,
        /// this will always happens.
        /// </summary>
        [DataMember(Name = STORE_MESSAGE_CONTENT_KEY, EmitDefaultValue = false)]
        public bool? StoreMessageContent { get; set; }
    }
}