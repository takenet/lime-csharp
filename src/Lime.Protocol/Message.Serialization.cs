using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    public partial class Message : IJsonWritable
    {

        #region IJsonWritable

        /// <summary>
        /// Writes the JSON representation
        /// of the object to the writer
        /// </summary>
        /// <param name="writer"></param>
        public override void WriteJson(IJsonWriter writer)
        {
            base.WriteJson(writer);
            writer.WriteProperty(TYPE_KEY, this.Type);
            writer.WriteProperty(CONTENT_KEY, this.Content);            
        }

        #endregion

        /// <summary>
        /// Creates an instance of the type
        /// using a dictionary that provides 
        /// the properties values
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        [Factory]
        public static Message FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                throw new ArgumentNullException("dictionary");
            }

            var message = new Message(null);
            Envelope.Populate(message, jsonObject);

            if (!jsonObject.ContainsKey(TYPE_KEY) ||
                !jsonObject.ContainsKey(CONTENT_KEY))
            {
                throw new ArgumentException("Invalid Message Json");
            }

            var mediaType = MediaType.Parse((string)jsonObject[TYPE_KEY]);
            var contentJsonObject = (JsonObject)jsonObject[CONTENT_KEY];

            message.Content = Document.FromJsonObject(contentJsonObject, mediaType);

            return message;
        }
    }
}
