using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Lime.Transport.Http.Storage
{
    public sealed class DictionaryEnvelopeStorage<T> : IEnvelopeStorage<T> where T : Envelope
    {
        private readonly ConcurrentDictionary<Identity, ConcurrentDictionary<string, T>> _identityEnvelopeDictionary;

        public DictionaryEnvelopeStorage()
        {
            _identityEnvelopeDictionary = new ConcurrentDictionary<Identity, ConcurrentDictionary<string, T>>();
        }

        #region IEnvelopeStorage<T> Members

        public Task<bool> StoreEnvelopeAsync(Identity owner, T envelope)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));            
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));            
            var envelopeDictionary = _identityEnvelopeDictionary.GetOrAdd(
                owner,
                (i) => new ConcurrentDictionary<string, T>());

            return Task.FromResult(envelopeDictionary.TryAdd(envelope.Id, envelope));
        }

        public Task<string[]> GetEnvelopesAsync(Identity owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            ConcurrentDictionary<string, T> envelopeDictionary;
            var envelopeIds = 
                _identityEnvelopeDictionary.TryGetValue(owner, out envelopeDictionary) ? 
                envelopeDictionary.Keys.ToArray() : new string[0];

            return Task.FromResult(envelopeIds);
        }

        public Task<T> GetEnvelopeAsync(Identity owner, string id)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            
            ConcurrentDictionary<string, T> envelopeDictionary;
            T envelope;            

            if (!(_identityEnvelopeDictionary.TryGetValue(owner, out envelopeDictionary) && envelopeDictionary.TryGetValue(id, out envelope)))
            {
                envelope = null;
            }

            return Task.FromResult(envelope);
        }

        public Task<bool> DeleteEnvelopeAsync(Identity owner, string id)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));            
            var deleted = false;
            ConcurrentDictionary<string, T> envelopeDictionary;

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