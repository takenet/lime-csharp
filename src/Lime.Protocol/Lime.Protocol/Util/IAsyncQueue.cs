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
        /// in the queue
        /// </summary>
        /// <param name="item"></param>
		bool Post(T item);

        /// <summary>
        /// Takes an Task which can be a promise
        /// to a instance of T or an available instance of
        /// T, if buffered.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T> ReceiveAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Informs if theres any 
        /// pending promise task in the
        /// queue
        /// </summary>
        bool HasPromises { get; }        

        /// <summary>
        /// Gets the total of enqueued items
        /// in the promises queue.
        /// </summary>
        int PromisesCount { get; }

        /// <summary>
        /// Gets the total of enqueued items
        /// in the buffer queue.
        /// </summary>
        int BufferCount { get; }
    }
}
