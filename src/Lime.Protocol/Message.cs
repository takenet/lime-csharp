using Lime.Protocol.Contents;
using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Lime.Protocol
{
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    [KnownType(typeof(TextContent))]
    public class Message : Envelope
    {
        /// <summary>
        ///  MIME declaration of the content type of the message.
        /// </summary>
        [DataMember(Name = "type", IsRequired = true)]
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
        [DataMember(Name = "content", IsRequired = true)]
        public Document Content { get; set; }

        internal static Envelope FromJsonObject(JsonObject jsonObject)
        {
            throw new NotImplementedException();
        }
    }
}