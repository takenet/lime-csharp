using System.Runtime.Serialization;

namespace Lime.Messaging.Resources.Checkout
{
    [DataContract]
    public enum CheckoutPaymentMethod
    {
        [EnumMember(Value = "Pix")]
        Pix,
    }
}
