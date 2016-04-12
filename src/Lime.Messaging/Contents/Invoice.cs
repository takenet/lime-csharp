using System;
using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents an invoice for requesting a payment.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Document" />
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Invoice : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.invoice+json";
        public const string NUMBER_KEY = "number";
        public const string TERMS_KEY = "terms";
        public const string ACCOUNT_KEY = "account";
        public const string CREATED_KEY = "created";
        public const string DUE_TO_KEY = "dueTo";
        public const string CURRENCY_KEY = "currency";
        public const string TAXES_KEY = "taxes";
        public const string TOTAL_KEY = "total";        
        public const string ITEMS_KEY = "items";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="Invoice"/> class.
        /// </summary>
        public Invoice() 
            : base(MediaType)
        {
        }

        /// <summary>
        /// Gets or sets the invoice number.
        /// </summary>
        /// <value>
        /// The number.
        /// </value>
        [DataMember(Name = NUMBER_KEY)]
        public string Number { get; set; }

        /// <summary>
        /// Gets or sets the invoice creation date.
        /// </summary>
        /// <value>
        /// The creation.
        /// </value>
        [DataMember(Name = CREATED_KEY)]
        public DateTimeOffset? Created { get; set; }

        /// <summary>
        /// Gets or sets the invoice expiration.
        /// </summary>
        /// <value>
        /// The expiration.
        /// </value>
        [DataMember(Name = DUE_TO_KEY)]
        public DateTimeOffset? DueTo { get; set; }

        /// <summary>
        /// Gets or sets the invoice payment terms description.
        /// </summary>
        /// <value>
        /// The terms.
        /// </value>
        [DataMember(Name = TERMS_KEY)]
        public string Terms { get; set; }

        /// <summary>
        /// Gets or sets the account information for the payment of the invoice.
        /// </summary>
        /// <value>
        /// The terms.
        /// </value>
        [DataMember(Name = ACCOUNT_KEY)]
        public string Account { get; set; }

        /// <summary>
        /// Gets or sets the invoice currency code related to the values.
        /// </summary>
        /// <value>
        /// The currency.
        /// </value>
        [DataMember(Name = CURRENCY_KEY)]
        public string Currency { get; set; }

        /// <summary>
        /// Gets or sets the invoice total taxes value.
        /// </summary>
        /// <value>
        /// The taxes.
        /// </value>
        [DataMember(Name = TAXES_KEY)]
        public decimal? Taxes { get; set; }

        /// <summary>
        /// Gets or sets the invoice total value, including taxes.
        /// </summary>
        /// <value>
        /// The total.
        /// </value>
        [DataMember(Name = TOTAL_KEY)]
        public decimal Total { get; set; }

        /// <summary>
        /// Gets or sets the invoice items.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        [DataMember(Name = ITEMS_KEY)]
        public InvoiceItem[] Items { get; set; }
    }

    /// <summary>
    /// Defines a invoice item.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class InvoiceItem
    {
        public const string QUANTITY_KEY = "quantity";
        public const string UNIT_KEY = "unit";
        public const string DESCRIPTION_KEY = "description";
        public const string CURRENCY_KEY = "currency";        
        public const string TAXES_KEY = "taxes";
        public const string TOTAL_KEY = "total";

        /// <summary>
        /// Gets or sets the item quantity.
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        [DataMember(Name = QUANTITY_KEY)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Gets or sets the item description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        [DataMember(Name = DESCRIPTION_KEY)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the item currency code related to the values.
        /// </summary>
        /// <value>
        /// The currency.
        /// </value>
        [DataMember(Name = CURRENCY_KEY)]
        public string Currency { get; set; }

        /// <summary>
        /// Gets or sets the unit value of each item.
        /// </summary>
        /// <value>
        /// The unit.
        /// </value>
        [DataMember(Name = UNIT_KEY)]
        public decimal Unit { get; set; }

        /// <summary>
        /// Gets or sets the taxes values for the item.
        /// </summary>
        /// <value>
        /// The taxes.
        /// </value>
        [DataMember(Name = TAXES_KEY)]
        public decimal? Taxes { get; set; }

        /// <summary>
        /// Gets or sets the total value of the items, including taxes.
        /// </summary>
        /// <value>
        /// The total.
        /// </value>
        [DataMember(Name = TOTAL_KEY)]
        public decimal Total { get; set; }
    }
}
