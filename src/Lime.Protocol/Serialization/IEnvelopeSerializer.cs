using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
    public interface IStreamEnvelopeSerializer
    {
        Task SerializeAsync(Envelope envelope, Stream stream, CancellationToken cancellationToken);

        Task<Envelope> DeserializeAsync(Stream stream, CancellationToken cancellationToken);
    }
}
