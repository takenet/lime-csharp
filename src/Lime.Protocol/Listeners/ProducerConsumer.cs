using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Listeners
{
    public static class ProducerConsumer
    {
        /// <summary>
        /// Creates and starts a long running task that listens the provided producer and call the consumer until the cancellation is requested.
        /// The cancellation occurs silently.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="producer">The producer func.</param>
        /// <param name="consumer">The consumer func.</param>
        /// <param name="cancellationToken">The cancellation token to stop the listener.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static Task<T> CreateAsync<T>(Func<CancellationToken, Task<T>> producer, Func<T, Task<bool>> consumer, CancellationToken cancellationToken)
        {
            if (producer == null) throw new ArgumentNullException(nameof(producer));
            if (consumer == null) throw new ArgumentNullException(nameof(consumer));            
            
            return Task.Run(async () =>
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var item = await producer(cancellationToken).ConfigureAwait(false);
                    if (cancellationToken.IsCancellationRequested ||
                        !await consumer(item).ConfigureAwait(false))
                    {
                        return item;
                    }
                }
            },
            cancellationToken);
        }
    }
}
