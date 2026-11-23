using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Resources.Checkout
{
    /// <summary>
    /// Represents the result of a <see cref="CustomerLastCardRequest"/>.
    /// </summary>
    [DataContract]
    public class CustomerLastCardResult : Document
    {
        /// <summary>
        /// The MIME type for the customer last card result document.
        /// </summary>
        public const string MIME_TYPE = "application/vnd.iris.payment.customer-last-card-result+json";

        /// <summary>
        /// The media type for the customer last card result document.
        /// </summary>
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomerLastCardResult"/> class.
        /// </summary>
        public CustomerLastCardResult()
            : base(MediaType) { }

        /// <summary>
        /// Gets or sets the version of the document contract.
        /// </summary>
        [DataMember(Name = "contractVersion")]
        public int? ContractVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer has a saved card. An absent value means no card.
        /// </summary>
        [DataMember(Name = "hasCard")]
        public bool? HasCard { get; set; }

        /// <summary>
        /// Gets or sets the saved card identifier.
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the last four digits of the saved card.
        /// </summary>
        [DataMember(Name = "lastFourDigits")]
        public string LastFourDigits { get; set; }
    }
}
