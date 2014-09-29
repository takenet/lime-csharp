using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Http.Storage
{
    public interface IEnvelopeStorage<T> where T : Envelope
    {
        Task<bool> StoreEnvelopeAsync(Identity owner, T envelope);

        Task<Guid[]> GetEnvelopesAsync(Identity owner);

        Task<T> GetEnvelopeAsync(Identity owner, Guid id);

        Task<bool> DeleteEnvelopeAsync(Identity owner, Guid id);
    }
}
