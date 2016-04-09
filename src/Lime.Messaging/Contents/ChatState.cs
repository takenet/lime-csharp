using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Allows the chat clients to exchange information about conversation events.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class ChatState : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.chatstate+json";
        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        public const string STATE_KEY = "state";

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatState"/> class.
        /// </summary>
        public ChatState()
            : base(MediaType)            
        {
        }

        /// <summary>
        /// Gets or sets the chat state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        [DataMember(Name = STATE_KEY)]
        public ChatStateEvent State { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => State.ToString();
    }

    /// <summary>
    /// The current chat state.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public enum ChatStateEvent
    {
        /// <summary>
        /// The other chat party started 
        /// a new chat a conversation.
        /// </summary>
        [EnumMember(Value = "starting")]
        Starting,
        /// <summary>
        /// The other party is typing.
        /// </summary>
        [EnumMember(Value = "composing")]
        Composing,
        /// <summary>
        /// The other party was 
        /// typing but stopped.
        /// </summary>
        [EnumMember(Value = "paused")]
        Paused,
        /// <summary>
        /// The other party is 
        /// deleting a text.
        /// </summary>
        [EnumMember(Value = "deleting")]
        Deleting,
        /// <summary>
        /// The other party 
        /// left the conversation.
        /// </summary>
        [EnumMember(Value = "gone")]
        Gone
    }
}
