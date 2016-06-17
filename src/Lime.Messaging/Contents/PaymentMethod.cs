using System.Runtime.Serialization;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Defines a payment method.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class PaymentMethod
    {
        public const string NAME_KEY = "name";
        public const string ACCOUNT_KEY = "account";

        /// <summary>
        /// Gets or sets the payment method name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [DataMember(Name = NAME_KEY)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the payment method account information of the seller.
        /// </summary>
        /// <value>
        /// The account.
        /// </value>
        [DataMember(Name = ACCOUNT_KEY)]
        public string Account { get; set; }
    }
}