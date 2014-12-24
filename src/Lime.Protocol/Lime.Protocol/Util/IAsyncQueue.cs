using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Util
{
    public interface IAsyncQueue<T>
    {
        /// <summary>
        /// Enqueue a new item 
        /// in the queue.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true if the item was successfully added; false otherwise.</returns>
		bool Post(T item);

        /// <summary>
        /// Tries to take an item from the queue, if available.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        bool TryTake(out T item);

        /// <summary>
        /// Enqueue a new item 
        /// in the queue, awaiting
        /// for space if not available.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> SendAsync(T item, CancellationToken cancellationToken);

        /// <summary>
        /// Takes an Task which can be a promise
        /// to a instance of T or an available instance of
        /// T, if buffered.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T> ReceiveAsync(CancellationToken cancellationToken);
    }
}
