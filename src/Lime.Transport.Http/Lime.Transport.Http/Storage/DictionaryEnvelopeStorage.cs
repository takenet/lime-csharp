using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Lime.Transport.Http.Protocol.Storage
{
    public sealed class DictionaryEnvelopeStorage<T> : IEnvelopeStorage<T> where T : Envelope
    {
        private ConcurrentDictionary<Identity, ConcurrentDictionary<Guid, T>> _identityEnvelopeDictionary;

        public DictionaryEnvelopeStorage()
        {
            _identityEnvelopeDictionary = new ConcurrentDictionary<Identity, ConcurrentDictionary<Guid, T>>();
        }

        #region IEnvelopeStorage<T> Members

        public Task<bool> StoreEnvelopeAsync(Identity owner, T envelope)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            if (envelope == null)
            {
                throw new ArgumentNullException("envelope");
            }

            var envelopeDictionary = _identityEnvelopeDictionary.GetOrAdd(
                owner,
                (i) => new ConcurrentDictionary<Guid, T>());

            return Task.FromResult(envelopeDictionary.TryAdd(envelope.Id, envelope));
        }

        public Task<Guid[]> GetEnvelopesAsync(Identity owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            Guid[] envelopeIds;
            ConcurrentDictionary<Guid, T> envelopeDictionary;

            if (_identityEnvelopeDictionary.TryGetValue(owner, out envelopeDictionary))
            {
                envelopeIds = envelopeDictionary.Keys.ToArray();
            }
            else
            {
                envelopeIds = new Guid[0];
            }

            return Task.FromResult(envelopeIds);
        }

        public Task<T> GetEnvelopeAsync(Identity owner, Guid id)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            ConcurrentDictionary<Guid, T> envelopeDictionary;
            T envelope;            

            if (!(_identityEnvelopeDictionary.TryGetValue(owner, out envelopeDictionary) && envelopeDictionary.TryGetValue(id, out envelope)))
            {
                envelope = null;
            }

            return Task.FromResult(envelope);
        }

        public Task<bool> DeleteEnvelopeAsync(Identity owner, Guid id)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            var deleted = false;

            ConcurrentDictionary<Guid, T> envelopeDictionary;

            if (_identityEnvelopeDictionary.TryGetValue(owner, out envelopeDictionary))
            {
                T envelope;
                deleted = envelopeDictionary.TryRemove(id, out envelope);
            }

            return Task.FromResult(deleted);
        }

        #endregion
    }
}