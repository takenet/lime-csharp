using System;
using System.Runtime.Serialization;

namespace Lime.Protocol
{
    /// <summary>
    /// Provides the transport of a content between nodes in a network.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Message : Envelope, IDocumentContainer
    {
        public const string TYPE_KEY = "type";
        public const string CONTENT_KEY = "content";

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        public Message()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public Message(string id)
            : base(id)
        {

        }       

        /// <summary>
        ///  MIME declaration of the content type of the message.
        /// </summary>
        [DataMember(Name = TYPE_KEY, IsRequired = true)]
        public MediaType Type => Content?.GetMediaType();

        /// <summary>
        /// Message body content
        /// </summary>
        [DataMember(Name = CONTENT_KEY, IsRequired = true)]
        public Document Content { get; set; }

        /// <summary>
        /// Gets the contained document.
        /// </summary>
        /// <returns></returns>
        Document IDocumentContainer.GetDocument() => Content;
    }
}