using System.Runtime.Serialization;

namespace Lime.Messaging.Resources.Checkout
{
    /// <summary>
    /// Represents the messages sent to the contact for the final statuses of a payment method.
    /// </summary>
    [DataContract]
    public class PaymentStatusMessages
    {
        /// <summary>
        /// The <c>default</c> value for <see cref="Mode"/>.
        /// </summary>
        public const string MODE_DEFAULT = "default";

        /// <summary>
        /// The <c>custom</c> value for <see cref="Mode"/>.
        /// </summary>
        public const string MODE_CUSTOM = "custom";

        /// <summary>
        /// Gets or sets the messages mode. See the <c>MODE_*</c> constants.
        /// </summary>
        [DataMember(Name = "mode")]
        public string Mode { get; set; }

        /// <summary>
        /// Gets or sets the message for an approved payment.
        /// </summary>
        [DataMember(Name = "approved")]
        public string Approved { get; set; }

        /// <summary>
        /// Gets or sets the message for a denied payment. Used by the payment link method.
        /// </summary>
        [DataMember(Name = "denied")]
        public string Denied { get; set; }

        /// <summary>
        /// Gets or sets the message for an expired payment. Used by the Pix method.
        /// </summary>
        [DataMember(Name = "expired")]
        public string Expired { get; set; }
    }
}
