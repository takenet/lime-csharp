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
    [Obsolete("Use JsonNetSerializer from Lime.Protocol.Serialization.Newtonsoft package instead")]
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
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            
            if (envelope is Notification)
            {
                return JsonSerializer<Notification>.Serialize((Notification)envelope);
            }
            else if (envelope is Message)
            {
                return JsonSerializer<Message>.Serialize((Message)envelope);
            }
            else if (envelope is Command)
            {
                return JsonSerializer<Command>.Serialize((Command)envelope);
            }
            else if (envelope is Session)
            {
                return JsonSerializer<Session>.Serialize((Session)envelope);
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
                return JsonSerializer<Message>.ParseJson(jsonObject);
            }
            else if (jsonObject.ContainsKey("event"))
            {
                return JsonSerializer<Notification>.ParseJson(jsonObject);
            }
            else if (jsonObject.ContainsKey("method"))
            {
                return JsonSerializer<Command>.ParseJson(jsonObject);
            }
            else if (jsonObject.ContainsKey("state"))
            {
                return JsonSerializer<Session>.ParseJson(jsonObject);
            }
            else
            {
                throw new ArgumentException("JSON string is not a valid envelope");
            }
        }

        #endregion
    }
}