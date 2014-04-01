using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Resources
{
    /// <summary>
    /// Represents an user account
    /// information
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Account : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.account+json";

        public Account()
            : base(MediaType.Parse(MIME_TYPE))
        {

        }

        /// <summary>
        /// The user full name
        /// </summary>
        [DataMember(Name = "fullName")]
        public string FullName { get; set; }

        /// <summary>
        ///  The user address
        /// </summary>
        [DataMember(Name = "address")]
        public string Address { get; set; }

        /// <summary>
        /// The user city
        /// </summary>
        [DataMember(Name = "city")]
        public string City { get; set; }

        /// <summary>
        /// The user e-mail address
        /// </summary>
        [DataMember(Name = "email")]
        public string Email { get; set; }

        /// <summary>
        /// The user phone number
        /// </summary>
        [DataMember(Name = "phoneNumber")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// The user cellphone number
        /// </summary>
        [DataMember(Name = "cellPhoneNumber")]
        public string CellPhoneNumber { get; set; }

        /// <summary>
        /// Indicates that the account is
        /// temporary is valid only in
        /// the current session
        /// </summary>
        [DataMember(Name = "isTemporary")]
        public bool? IsTemporary { get; set; }

        /// <summary>
        /// Base64 representation of the 
        /// account password
        /// </summary>
        [DataMember(Name = "password")]
        public string Password { get; set; }

        /// <summary>
        /// Base64 representation of the 
        /// account password. Mandatory
        /// in case of updating account
        /// password.
        /// </summary>
        [DataMember(Name = "oldPassword")]
        public string OldPassword { get; set; }

        /// <summary>
        /// Size of account inbox
        /// for storing offline messages
        /// </summary>
        [DataMember(Name = "inboxSize", EmitDefaultValue = false)]
        public int? InboxSize { get; set; }

        /// <summary>
        /// Indicates if this account
        /// allows receive messages from
        /// anonymous users
        /// </summary>
        [DataMember(Name = "allowAnonymousSender", EmitDefaultValue = false)]
        public bool? AllowAnonymousSender { get; set; }

        /// <summary>
        /// Indicates if this account
        /// allows receive messages from
        /// users that are not in
        /// the account contact list
        /// </summary>
        [DataMember(Name = "allowUnknownSender", EmitDefaultValue = false)]
        public bool? AllowUnknownSender { get; set; }

        /// <summary>
        /// Indicates if the content of messages 
        /// from this account should be stored in 
        /// the server. Note that for offline messages,
        /// this will always happens.
        /// </summary>
        [DataMember(Name = "storeMessageContent", EmitDefaultValue = false)]
        public bool? StoreMessageContent { get; set; }
    }
}