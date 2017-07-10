using System.Runtime.Serialization;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Defines an invoice item.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class InvoiceItem : Item
    {
        public const string COMPLEMENT_KEY = "complement";
        public const string QUANTITY_KEY = "quantity";
        public const string UNIT_KEY = "unit";
        public const string CURRENCY_KEY = "currency";        
        public const string TAXES_KEY = "taxes";
        public const string TOTAL_KEY = "total";

        /// <summary>
        /// Gets or sets the item description complement.
        /// </summary>
        /// <value>
        /// The description complement
        /// </value>
        [DataMember(Name = COMPLEMENT_KEY)]
        public string Complement { get; set; }

        /// <summary>
        /// Gets or sets the item quantity.
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        [DataMember(Name = QUANTITY_KEY)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Gets or sets the unit value of each item.
        /// </summary>
        /// <value>
        /// The unit.
        /// </value>
        [DataMember(Name = UNIT_KEY)]
        public decimal Unit { get; set; }

        /// <summary>
        /// Gets or sets the item currency code related to the values.
        /// </summary>
        /// <value>
        /// The currency.
        /// </value>
        [DataMember(Name = CURRENCY_KEY)]
        public string Currency { get; set; }

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