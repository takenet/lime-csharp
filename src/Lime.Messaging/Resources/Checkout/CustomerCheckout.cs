using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Lime.Messaging.Resources.Checkout
{
    /// <summary>
    /// Represents the customer information required for a checkout process.
    /// </summary>
    public class CustomerCheckout
    {
        /// <summary>
        /// Gets or sets the unique identity of the customer.
        /// </summary>
        [DataMember(Name = "identity")]
        public string Identity { get; set; }

        /// <summary>
        /// Gets or sets the phone number of the customer.
        /// </summary>
        [DataMember(Name = "phone")]
        public string Phone { get; set; }

        /// <summary>
        /// Gets or sets the first name of the customer.
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the surname of the customer.
        /// </summary>
        [DataMember(Name = "surname")]
        public string Surname { get; set; }

        /// <summary>
        /// Gets or sets the email address of the customer.
        /// </summary>
        [DataMember(Name = "email")]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the type of document provided by the customer (e.g., passport, ID card).
        /// </summary>
        [DataMember(Name = "documentType")]
        public string DocumentType { get; set; }

        /// <summary>
        /// Gets or sets the document number provided by the customer.
        /// </summary>
        [DataMember(Name = "document")]
        public string Document { get; set; }
    }
}
