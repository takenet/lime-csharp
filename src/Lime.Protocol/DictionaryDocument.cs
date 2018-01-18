using System;
using System.Collections;
using System.Collections.Generic;

namespace Lime.Protocol
{
    public class DictionaryDocument<TKey, TValue> : Document, IDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _json;

        public DictionaryDocument(MediaType mediaType)
            : this(new Dictionary<TKey, TValue>(), mediaType)
        {

        }

        public DictionaryDocument(IDictionary<TKey, TValue> json, MediaType mediaType)
            : base(mediaType)
        {            
            _json = json ?? throw new ArgumentNullException(nameof(json));

            if (!mediaType.IsJson)
            {
                throw new ArgumentException("The media type is not a valid json type");
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _json.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _json).GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _json.Add(item);
        }

        public void Clear()
        {
            _json.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _json.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _json.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return _json.Remove(item);
        }

        public int Count => _json.Count;

        public bool IsReadOnly => _json.IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            _json.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return _json.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            return _json.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _json.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get => _json[key];
            set => _json[key] = value;
        }
        public ICollection<TKey> Keys => _json.Keys;

        public ICollection<TValue> Values => _json.Values;
    }
}