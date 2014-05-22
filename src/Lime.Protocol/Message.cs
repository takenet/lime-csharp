using Lime.Protocol.Contents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Lime.Protocol
{
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public partial class Message : Envelope
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
                if (this.Content != null)
                {
                    return this.Content.GetMediaType();
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