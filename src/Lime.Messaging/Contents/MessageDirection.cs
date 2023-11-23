using System.Runtime.Serialization;

namespace Lime.Messaging.Contents
{
    [DataContract]
    public enum MessageDirection
    {
        /// <summary>
        /// The message was sent by the thread owner.
        /// </summary>
        [EnumMember(Value = "sent")]
        Sent,
        /// <summary>
        /// The message was received by the thread owner.
        /// </summary>
        [EnumMember(Value = "received")]
        Received
    }
}
