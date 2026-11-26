using System.Runtime.Serialization;

namespace Lime.Messaging.Resources.Checkout
{
    /// <summary>
    /// Represents the Pix data of a <see cref="PaymentCheckoutResult"/>.
    /// </summary>
    [DataContract]
    public class PaymentCheckoutPix
    {
        /// <summary>
        /// Gets or sets the Pix copy and paste code.
        /// </summary>
        [DataMember(Name = "code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the expiration date, written in ISO 8601 UTC format.
        /// When read by the Newtonsoft envelope serializer, the value may arrive in the invariant date format
        /// (for example, <c>12/01/2026 12:00:00</c>); readers should parse it with the invariant culture and as UTC.
        /// </summary>
        [DataMember(Name = "expiresAt")]
        public string ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the name of the merchant shown to the payer.
        /// </summary>
        [DataMember(Name = "merchantName")]
        public string MerchantName { get; set; }

        /// <summary>
        /// Gets or sets the Pix key of the merchant.
        /// </summary>
        [DataMember(Name = "key")]
        public string Key { get; set; }
    }
}
