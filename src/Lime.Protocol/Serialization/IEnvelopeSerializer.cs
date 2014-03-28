using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Base interface for envelope
    /// serializers
    /// </summary>
    public interface IEnvelopeSerializer
    {
        /// <summary>
        /// Serialize an envelope
        /// to a string
        /// </summary>
        /// <typeparam name="TEnvelope"></typeparam>
        /// <param name="envelope"></param>
        /// <returns></returns>
        string Serialize(Envelope envelope);

        /// <summary>
        /// Deserialize an envelope
        /// from a string
        /// </summary>
        /// <typeparam name="TEnvelope"></typeparam>
        /// <param name="envelopeString"></param>
        /// <returns></returns>
        Envelope Deserialize(string envelopeString);
    }
}
