using System;
using System.Runtime.Serialization;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Defines an invoice address.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class PaymentAddress
    {
        public const string STREET_KEY = "street";
        public const string CITY_KEY = "city";
        public const string POSTAL_CODE_KEY = "postalCode";
        public const string STATE_KEY = "state";
        public const string COUNTRY_KEY = "country";

        /// <summary>
        /// Gets or sets the street and number on a payment receipt
        /// </summary>
        [DataMember(Name = STREET_KEY)]
        public string Street { get; set; }

        /// <summary>
        /// Gets or sets the city on a payment receipt
        /// </summary>
        [DataMember(Name = CITY_KEY)]
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the postal code on a payment receipt
        /// </summary>
        [DataMember(Name = POSTAL_CODE_KEY)]
        public string PostalCode { get; set; }

        /// <summary>
        /// Gets or sets the state on a payment receipt
        /// </summary>
        [DataMember(Name = STATE_KEY)]
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the country on a payment receipt
        /// </summary>
        [DataMember(Name = COUNTRY_KEY)]
        public string Country { get; set; }
    }
}
