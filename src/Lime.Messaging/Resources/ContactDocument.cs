using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Resources
{
    /// <summary>
    /// Base class for contact information data.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Document" />
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public abstract class ContactDocument : Document
    {
        public const string ADDRESS_KEY = "address";
        public const string CITY_KEY = "city";
        public const string EMAIL_KEY = "email";
        public const string PHONE_NUMBER_KEY = "phoneNumber";
        public const string PHOTO_URI_KEY = "photoUri";
        public const string CELL_PHONE_NUMBER_KEY = "cellPhoneNumber";
        public const string GENDER_KEY = "gender";
        public const string TIMEZONE_KEY = "timezone";
        public const string CULTURE_KEY = "culture";
        public const string EXTRAS_KEY = "extras";

        /// <summary>
        /// Initializes a new instance of the <see cref="ContactDocument"/> class.
        /// </summary>
        /// <param name="mediaType">Type of the media.</param>
        protected ContactDocument(MediaType mediaType) 
            : base(mediaType)
        {
        }

        /// <summary>
        /// The user street address.
        /// </summary>
        [DataMember(Name = ADDRESS_KEY)]
        public string Address { get; set; }

        /// <summary>
        /// The user city.
        /// </summary>
        [DataMember(Name = CITY_KEY)]
        public string City { get; set; }

        /// <summary>
        /// The user e-mail address.
        /// </summary>
        [DataMember(Name = EMAIL_KEY)]
        public string Email { get; set; }

        /// <summary>
        /// The user phone number.
        /// </summary>
        [DataMember(Name = PHONE_NUMBER_KEY)]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// The user photo URI.
        /// </summary>
        [DataMember(Name = PHOTO_URI_KEY)]
        public Uri PhotoUri { get; set; }

        /// <summary>
        /// The user cellphone number
        /// </summary>
        [DataMember(Name = CELL_PHONE_NUMBER_KEY)]
        public string CellPhoneNumber { get; set; }

        /// <summary>
        /// Represents the person account gender.
        /// </summary>
        [DataMember(Name = GENDER_KEY)]
        public Gender? Gender { get; set; }

        /// <summary>
        /// Represents the account timezone relative to GMT.
        /// </summary>
        [DataMember(Name = TIMEZONE_KEY)]
        public int? Timezone { get; set; }

        /// <summary>
        /// Represents the person account culture info, in the IETF language tag format.
        /// <a href="https://en.wikipedia.org/wiki/IETF_language_tag"/>.
        /// </summary>
        [DataMember(Name = CULTURE_KEY)]
        public string Culture { get; set; }

        /// <summary>
        /// Gets or sets the contact extra information.
        /// </summary>        
        [DataMember(Name = EXTRAS_KEY)]
        public IDictionary<string, string> Extras { get; set; }
    }

    /// <summary>
    /// Represents the account person gender
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum Gender
    {
        /// <summary>
        /// The male gender
        /// </summary>
        [EnumMember(Value = "male")]
        Male,
        /// <summary>
        /// The female gender
        /// </summary>
        [EnumMember(Value = "female")]
        Female
    }
}
