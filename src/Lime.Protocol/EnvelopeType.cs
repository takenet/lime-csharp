using System.Runtime.Serialization;

namespace Lime.Protocol
{
    /// <summary>
    /// Lists the protocol supported envelope types.
    /// </summary>
    [DataContract]
    public enum EnvelopeType
    {
        /// <summary>
        /// The message envelope type,
        /// used for content transport.
        /// </summary>
        [EnumMember(Value = "message")]
        Message,
        /// <summary>
        /// The notification envelope type,
        /// used to notify events about messages.
        /// </summary>
        [EnumMember(Value = "notification")]
        Notification,
        /// <summary>
        /// The command envelope type,
        /// used for request/response request processing.
        /// </summary>
        [EnumMember(Value = "command")]
        Command,
        /// <summary>
        /// The session envelope type,
        /// used for the communication channel establishment.
        /// </summary>
        [EnumMember(Value = "session")]
        Session
    }
}
