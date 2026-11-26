using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Resources.Checkout
{
    /// <summary>
    /// Represents a request for the last card saved by the customer of a purchase.
    /// </summary>
    [DataContract]
    public class CustomerLastCardRequest : Document
    {
        /// <summary>
        /// The MIME type for the customer last card request document.
        /// </summary>
        public const string MIME_TYPE = "application/vnd.iris.payment.customer-last-card-request+json";

        /// <summary>
        /// The media type for the customer last card request document.
        /// </summary>
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomerLastCardRequest"/> class.
        /// </summary>
        public CustomerLastCardRequest()
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
    }
}
