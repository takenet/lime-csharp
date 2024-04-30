using System.Buffers;
using System.IO;

namespace Lime.Protocol.Serialization
{
    /// <summary>
    /// Base interface for envelope serializers.
    /// </summary>
    public interface IEnvelopeSerializer
    {
        /// <summary>
        /// Serialize an envelope to a string.
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        string Serialize(Envelope envelope);

        /// <summary>
        /// Deserialize an envelope from a string.
        /// </summary>
        /// <param name="envelopeString"></param>
        /// <returns></returns>
        Envelope Deserialize(string envelopeString);

        /// <summary>
        /// Deserialize an envelope from a text reader.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        T Deserialize<T>(TextReader reader)
            where T : Envelope;
    }
}
