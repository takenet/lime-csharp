using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lime.Protocol
{
    /// <summary>
    /// Utilities extensions for the <c>IEnumerable</c> interface
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Creates a new <c>DocumentCollection</c> object using a given <paramref name="documentEnumerable"/>
        /// </summary>
        /// <returns>The document collection.</returns>
        /// <param name="documentEnumerable"><c>IEnumerable</c> of <c>Document</c>.</param>
        /// <param name="size">The number of <c>Documents</c> in the collection</param>
        /// <typeparam name="T">Subtype of the <c>Document</c>'s contained in the <c>IEnumerable</c></typeparam>
        public static DocumentCollection ToDocumentCollection<T>(this IEnumerable<T> documentEnumerable, int size = 0) where T : Document
        {
            return new DocumentCollection
            {
                Items = documentEnumerable.ToArray(),
                ItemType = documentEnumerable.FirstOrDefault().GetMediaType(),
                Total = size == 0 ? documentEnumerable.Count() : size
            };
        }
    }
}
