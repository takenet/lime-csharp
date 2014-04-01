using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Contents
{
    /// <summary>
    /// Allows the chat clients to exchange 
    /// information about conversation events.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class ChatState : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.chatstate+json";

        public ChatState()
            : base(MediaType.Parse(MIME_TYPE))            
        {
        }

        [DataMember(Name = "state")]
        public ChatStateEvent State { get; set; }        
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
