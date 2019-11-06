using System.Buffers;

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
    }

    /// <summary>
    /// Defines a serializer that doesn't requires buffer allocation.  
    /// </summary>
    public interface IMemoryEnvelopeSerializer
    {
        ReadOnlySequence<byte> Serialize(Envelope envelope);

        Envelope Deserialize(ReadOnlySequence<byte> envelopeBytes);
    }
}
