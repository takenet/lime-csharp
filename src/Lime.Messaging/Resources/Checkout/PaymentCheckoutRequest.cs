using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Resources.Checkout
{
    /// <summary>
    /// Represents a request to create a checkout with one or more payment methods.
    /// </summary>
    [DataContract]
    public class PaymentCheckoutRequest : Document
    {
        /// <summary>
        /// The MIME type for the payment checkout request document.
        /// </summary>
        public const string MIME_TYPE = "application/vnd.iris.payment.checkout-request+json";

        /// <summary>
        /// The <c>whatsapp</c> value for <see cref="Channel"/>.
        /// </summary>
        public const string CHANNEL_WHATSAPP = "whatsapp";

        /// <summary>
        /// The <c>messenger</c> value for <see cref="Channel"/>.
        /// </summary>
        public const string CHANNEL_MESSENGER = "messenger";

        /// <summary>
        /// The <c>instagram</c> value for <see cref="Channel"/>.
        /// </summary>
        public const string CHANNEL_INSTAGRAM = "instagram";

        /// <summary>
        /// The <c>blipchat</c> value for <see cref="Channel"/>.
        /// </summary>
        public const string CHANNEL_BLIPCHAT = "blipchat";

        /// <summary>
        /// The <c>other</c> value for <see cref="Channel"/>.
        /// </summary>
        public const string CHANNEL_OTHER = "other";

        /// <summary>
        /// The <c>BRL</c> value for <see cref="Currency"/>.
        /// </summary>
        public const string CURRENCY_BRL = "BRL";

        /// <summary>
        /// The <c>payment_link</c> value for <see cref="PaymentMethods"/>.
        /// </summary>
        public const string PAYMENT_METHOD_PAYMENT_LINK = "payment_link";

        /// <summary>
        /// The <c>pix</c> value for <see cref="PaymentMethods"/>.
        /// </summary>
        public const string PAYMENT_METHOD_PIX = "pix";

        /// <summary>
        /// The <c>digital</c> value for <see cref="ProductType"/>.
        /// </summary>
        public const string PRODUCT_TYPE_DIGITAL = "digital";

        /// <summary>
        /// The <c>physical</c> value for <see cref="ProductType"/>.
        /// </summary>
        public const string PRODUCT_TYPE_PHYSICAL = "physical";

        /// <summary>
        /// The media type for the payment checkout request document.
        /// </summary>
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentCheckoutRequest"/> class.
        /// </summary>
        public PaymentCheckoutRequest()
            : base(MediaType) { }

        /// <summary>
        /// Gets or sets the version of the document contract.
        /// </summary>
        [DataMember(Name = "contractVersion")]
        public int? ContractVersion { get; set; }

        /// <summary>
        /// Gets or sets the key that identifies retries of the same request.
        /// </summary>
        [DataMember(Name = "idempotencyKey")]
        public string IdempotencyKey { get; set; }

        /// <summary>
        /// Gets or sets the identity of the contact that is paying.
        /// </summary>
        [DataMember(Name = "contactIdentity")]
        public string ContactIdentity { get; set; }

        /// <summary>
        /// Gets or sets the channel of the conversation. See the <c>CHANNEL_*</c> constants.
        /// </summary>
        [DataMember(Name = "channel")]
        public string Channel { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the flow block that sent the request.
        /// </summary>
        [DataMember(Name = "blockId")]
        public string BlockId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the merchant.
        /// </summary>
        [DataMember(Name = "merchantId")]
        public string MerchantId { get; set; }

        /// <summary>
        /// Gets or sets the currency code. See <see cref="CURRENCY_BRL"/>.
        /// </summary>
        [DataMember(Name = "currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Gets or sets the items of the checkout.
        /// </summary>
        [DataMember(Name = "items")]
        public PaymentCheckoutItem[] Items { get; set; }

        /// <summary>
        /// Gets or sets the payment methods to generate. See the <c>PAYMENT_METHOD_*</c> constants.
        /// </summary>
        [DataMember(Name = "paymentMethods")]
        public string[] PaymentMethods { get; set; }

        /// <summary>
        /// Gets or sets the status messages for the payment link method.
        /// </summary>
        [DataMember(Name = "linkMessages")]
        public PaymentStatusMessages LinkMessages { get; set; }

        /// <summary>
        /// Gets or sets the status messages for the Pix method.
        /// </summary>
        [DataMember(Name = "pixMessages")]
        public PaymentStatusMessages PixMessages { get; set; }

        /// <summary>
        /// Gets or sets the product type. See the <c>PRODUCT_TYPE_*</c> constants.
        /// </summary>
        [DataMember(Name = "productType")]
        public string ProductType { get; set; }
    }
}
