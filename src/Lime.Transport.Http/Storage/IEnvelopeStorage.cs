using System;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Lime.Transport.Http.Storage
{
    public interface IEnvelopeStorage<T> where T : Envelope
    {
        Task<bool> StoreEnvelopeAsync(Identity owner, T envelope);

        Task<string[]> GetEnvelopesAsync(Identity owner);

        Task<T> GetEnvelopeAsync(Identity owner, string id);

        Task<bool> DeleteEnvelopeAsync(Identity owner, string id);
    }
}
