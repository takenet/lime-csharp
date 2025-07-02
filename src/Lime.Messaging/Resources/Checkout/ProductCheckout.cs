using System.Runtime.Serialization;

namespace Lime.Messaging.Resources.Checkout
{
    /// <summary>
    /// Represents a product in a checkout process.
    /// </summary>
    [DataContract]
    public class ProductCheckout
    {
        /// <summary>
        /// Gets or sets the description of the product.
        /// </summary>
        [DataMember(Name = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the price of the product.
        /// </summary>
        [DataMember(Name = "price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the quantity of the product.
        /// </summary>
        [DataMember(Name = "quantity")]
        public int Quantity { get; set; }
    }
}
