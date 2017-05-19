using Lime.Protocol;
using System.Runtime.Serialization;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Indicates to a node to redirect to another address in order to continue the current conversation.
    /// It is useful to handover the current conversation to another connected nodes.
    /// </summary>
    public class Redirect : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.redirect+json";
        public const string ADDRESS_KEY = "address";
        public const string CONTEXT_KEY = "context";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="Redirect"/> class.
        /// </summary>
        public Redirect()
            : base(MediaType)
        {
        }

        /// <summary>
        /// Gets or sets the redirect address.
        /// </summary>
        [DataMember(Name = ADDRESS_KEY)]
        public Node Address { get; set; }

        /// <summary>
        /// Gets or sets the state data to be forwarded to the redirect address in order to keep the conversation context.
        /// </summary>
        [DataMember(Name = CONTEXT_KEY)]
        public DocumentContainer Context { get; set; }
    }
}
