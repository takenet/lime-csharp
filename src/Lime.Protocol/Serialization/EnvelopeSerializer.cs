using Lime.Protocol.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Default protocol serialization
    /// </summary>
    public class EnvelopeSerializer : IEnvelopeSerializer
    {
        #region IEnvelopeSerializer Members

        /// <summary>
        /// Serialize an envelope
        /// to a string
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        public string Serialize(Envelope envelope)
        {            
            return envelope.ToJson();
        }

        /// <summary>
        /// Deserialize an envelope
        /// from a string
        /// </summary>
        /// <param name="envelopeString"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">JSON string is not a valid envelope</exception>
        public Envelope Deserialize(string envelopeString)
        {
            var jsonDictionary = JsonObject.ParseJson(envelopeString);

            if (jsonDictionary.ContainsKey("content"))
            {
                return Message.FromJsonObject(jsonDictionary);
            }
            else if (jsonDictionary.ContainsKey("event"))
            {
                return Notification.FromJsonObject(jsonDictionary);
            }
            else if (jsonDictionary.ContainsKey("method"))
            {
                return Command.FromJsonObject(jsonDictionary);
            }
            else if (jsonDictionary.ContainsKey("state"))
            {
                return Session.FromJsonObject(jsonDictionary);
            }
            else
            {
                throw new ArgumentException("JSON string is not a valid envelope");
            }
        }

        #endregion
    }
}