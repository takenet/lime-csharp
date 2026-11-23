using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Resources.Checkout
{
    /// <summary>
    /// Represents a request to pay a purchase with a saved card.
    /// </summary>
    [DataContract]
    public class SavedCardPaymentRequest : Document
    {
        /// <summary>
        /// The MIME type for the saved card payment request document.
        /// </summary>
        public const string MIME_TYPE = "application/vnd.iris.payment.saved-card-payment-request+json";

        /// <summary>
        /// The media type for the saved card payment request document.
        /// </summary>
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="SavedCardPaymentRequest"/> class.
        /// </summary>
        public SavedCardPaymentRequest()
            : base(MediaType) { }

        /// <summary>
        /// Gets or sets the version of the document contract.
        /// </summary>
        [DataMember(Name = "contractVersion")]
        public int? ContractVersion { get; set; }

        /// <summary>
        /// Gets or sets the purchase identifier.
        /// </summary>
        [DataMember(Name = "purchaseId")]
        public string PurchaseId { get; set; }

        /// <summary>
        /// Gets or sets the saved card identifier.
        /// </summary>
        [DataMember(Name = "cardId")]
        public string CardId { get; set; }

        /// <summary>
        /// Gets or sets the key that identifies retries of the same request.
        /// </summary>
        [DataMember(Name = "idempotencyKey")]
        public string IdempotencyKey { get; set; }
    }
}
