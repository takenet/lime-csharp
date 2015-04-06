using System;
using System.Runtime.Serialization;

namespace Lime.Protocol
{
    /// <summary>
    /// Provides the transport of a content 
    /// between nodes in a network.
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public class Message : Envelope
    {
        public const string TYPE_KEY = "type";
        public const string CONTENT_KEY = "content";

        #region Constructor

        public Message()
        {
        }

        public Message(Guid id)
            : base(id)
        {
        }

        #endregion

        /// <summary>
        ///  MIME declaration of the content type of the message.
        /// </summary>
        [DataMember(Name = TYPE_KEY, IsRequired = true)]
        public MediaType Type
        {
            get 
            {
                if (Content != null)
                {
                    return Content.GetMediaType();
                }

                return null;
            }
        }

        /// <summary>
        /// Message body content
        /// </summary>
        [DataMember(Name = CONTENT_KEY, IsRequired = true)]
        public Document Content { get; set; }
    }
}