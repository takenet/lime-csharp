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
            if (envelope == null)
            {
                throw new ArgumentNullException("envelope");
            }

            if (envelope is Notification)
            {
                return TypeSerializer<Notification>.Serialize((Notification)envelope);
            }
            else if (envelope is Message)
            {
                return TypeSerializer<Message>.Serialize((Message)envelope);
            }
            else if (envelope is Command)
            {
                return TypeSerializer<Command>.Serialize((Command)envelope);
            }
            else if (envelope is Session)
            {
                return TypeSerializer<Session>.Serialize((Session)envelope);
            }
            else
            {
                throw new ArgumentException("The envelope type is unknown");
            }
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
            var jsonObject = JsonObject.ParseJson(envelopeString);

            if (jsonObject.ContainsKey("content"))
            {
                return TypeSerializer<Message>.ParseJson(jsonObject);
            }
            else if (jsonObject.ContainsKey("event"))
            {
                return TypeSerializer<Notification>.ParseJson(jsonObject);
            }
            else if (jsonObject.ContainsKey("method"))
            {
                return TypeSerializer<Command>.ParseJson(jsonObject);
            }
            else if (jsonObject.ContainsKey("state"))
            {
                return TypeSerializer<Session>.ParseJson(jsonObject);
            }
            else
            {
                throw new ArgumentException("JSON string is not a valid envelope");
            }
        }

        #endregion
    }
}