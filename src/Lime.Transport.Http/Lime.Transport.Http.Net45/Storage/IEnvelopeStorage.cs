using System;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Lime.Transport.Http.Storage
{
    public interface IEnvelopeStorage<T> where T : Envelope
    {
        Task<bool> StoreEnvelopeAsync(Identity owner, T envelope);

        Task<Guid[]> GetEnvelopesAsync(Identity owner);

        Task<T> GetEnvelopeAsync(Identity owner, Guid id);

        Task<bool> DeleteEnvelopeAsync(Identity owner, Guid id);
    }
}
