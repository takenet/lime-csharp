using Lime.Protocol;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Lime.Messaging.Contents.Checkout
{
    /// <summary>
    /// Represents a checkout document containing customer, product, and transaction details.
    /// </summary>
    [DataContract]
    public class CheckoutDocument : Document
    {
        /// <summary>
        /// The MIME type for the checkout document.
        /// </summary>
        public const string MIME_TYPE = "application/vnd.iris.builder.checkout+json";

        /// <summary>
        /// The media type for the checkout document.
        /// </summary>
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckoutDocument"/> class.
        /// </summary>
        public CheckoutDocument()
            : base(MediaType) { }

        /// <summary>
        /// Gets or sets the customer information for the checkout.
        /// </summary>
        /// <value>
        /// An instance of <see cref="CustomerCheckout"/> containing customer details.
        /// </value>
        [DataMember(Name = "customer")]
        public CustomerCheckout Customer { get; set; }

        /// <summary>
        /// Gets or sets the list of products included in the checkout.
        /// </summary>
        /// <value>
        /// A list of <see cref="ProductCheckout"/> representing the products.
        /// </value>
        [DataMember(Name = "products")]
        public List<ProductCheckout> Products { get; set; }

        /// <summary>
        /// Gets or sets the currency used for the transaction.
        /// </summary>
        /// <value>
        /// A string representing the currency code (e.g., "USD", "EUR").
        /// </value>
        [DataMember(Name = "currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Gets or sets the channel through which the checkout was initiated.
        /// </summary>
        /// <value>
        /// A string representing the channel (e.g., "web", "mobile").
        /// </value>
        [DataMember(Name = "channel")]
        public string Channel { get; set; }

        /// <summary>
        /// Gets or sets the source of the checkout request.
        /// </summary>
        /// <value>
        /// A string representing the source (e.g., "app", "store").
        /// </value>
        [DataMember(Name = "source")]
        public string Source { get; set; }

        /// <summary>
        /// Creates a shallow copy of the current <see cref="CheckoutDocument"/> instance.
        /// </summary>
        /// <returns></returns>
        public CheckoutDocument ShallowCopy()
        {
            return (CheckoutDocument)MemberwiseClone();
        }
    }
}
