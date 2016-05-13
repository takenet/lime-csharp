using System.Collections.Generic;

namespace Lime.Protocol.Serialization
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Removes the element with the specified key from the dictionary and  adds an element with the provided key and value.
        /// Note: This call is not synchronized.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void RemoveAndAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            dictionary.Remove(key);
            dictionary.Add(key, value);
        }
    }
}