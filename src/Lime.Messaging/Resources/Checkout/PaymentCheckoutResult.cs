using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Resources.Checkout
{
    /// <summary>
    /// Represents the result of a <see cref="PaymentCheckoutRequest"/>.
    /// </summary>
    [DataContract]
    public class PaymentCheckoutResult : Document
    {
        /// <summary>
        /// The MIME type for the payment checkout result document.
        /// </summary>
        public const string MIME_TYPE = "application/vnd.iris.payment.checkout-result+json";

        /// <summary>
        /// The <c>created</c> value for <see cref="Status"/>.
        /// </summary>
        public const string STATUS_CREATED = "created";

        /// <summary>
        /// The media type for the payment checkout result document.
        /// </summary>
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentCheckoutResult"/> class.
        /// </summary>
        public PaymentCheckoutResult()
            : base(MediaType) { }

        /// <summary>
        /// Gets or sets the version of the document contract.
        /// </summary>
        [DataMember(Name = "contractVersion")]
        public int? ContractVersion { get; set; }

        /// <summary>
        /// Gets or sets the checkout identifier, as a GUID with hyphens.
        /// </summary>
        [DataMember(Name = "checkoutId")]
        public string CheckoutId { get; set; }

        /// <summary>
        /// Gets or sets the checkout identifier without hyphens.
        /// </summary>
        [DataMember(Name = "referenceId")]
        public string ReferenceId { get; set; }

        /// <summary>
        /// Gets or sets the checkout status. See the <c>STATUS_*</c> constants.
        /// </summary>
        [DataMember(Name = "status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the payment link, when the payment link method was requested.
        /// </summary>
        [DataMember(Name = "paymentLink")]
        public string PaymentLink { get; set; }

        /// <summary>
        /// Gets or sets the Pix data, when the Pix method was requested.
        /// </summary>
        [DataMember(Name = "pix")]
        public PaymentCheckoutPix Pix { get; set; }
    }
}
