using System.Runtime.Serialization;

namespace Lime.Messaging.Resources.Checkout
{
    /// <summary>
    /// Represents an item of a <see cref="PaymentCheckoutRequest"/>.
    /// </summary>
    [DataContract]
    public class PaymentCheckoutItem
    {
        /// <summary>
        /// Gets or sets the item description.
        /// </summary>
        [DataMember(Name = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the item quantity.
        /// </summary>
        [DataMember(Name = "quantity")]
        public int? Quantity { get; set; }

        /// <summary>
        /// Gets or sets the unit amount, in cents.
        /// </summary>
        [DataMember(Name = "amount")]
        public long? Amount { get; set; }
    }
}
