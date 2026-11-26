using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Resources.Checkout
{
    /// <summary>
    /// Represents the result of a <see cref="SavedCardPaymentRequest"/>.
    /// </summary>
    [DataContract]
    public class SavedCardPaymentResult : Document
    {
        /// <summary>
        /// The MIME type for the saved card payment result document.
        /// </summary>
        public const string MIME_TYPE = "application/vnd.iris.payment.saved-card-payment-result+json";

        /// <summary>
        /// The <c>processing</c> value for <see cref="Status"/>.
        /// </summary>
        public const string STATUS_PROCESSING = "processing";

        /// <summary>
        /// The media type for the saved card payment result document.
        /// </summary>
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="SavedCardPaymentResult"/> class.
        /// </summary>
        public SavedCardPaymentResult()
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
        /// Gets or sets the payment status. See the <c>STATUS_*</c> constants.
        /// </summary>
        [DataMember(Name = "status")]
        public string Status { get; set; }
    }
}
