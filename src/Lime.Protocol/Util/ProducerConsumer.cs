using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Util
{
    public static class ProducerConsumer
    {
        /// <summary>
        /// Creates and starts a task that listens the provided producer and call the consumer until the cancellation is requested.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="producer">The producer func.</param>
        /// <param name="consumer">The consumer func.</param>
        /// <param name="cancellationToken">The cancellation token for the consumer task.</param>
        /// <param name="handleCancellation">Indicates if the <see cref="OperationCanceledException"/> should be handled if the provided cancellationToken is cancelled.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static Task<T> CreateAsync<T>(
            Func<CancellationToken, Task<T>> producer,
            Func<T, CancellationToken, Task<bool>> consumer, 
            CancellationToken cancellationToken, 
            bool handleCancellation = false)
        {
            if (producer == null) throw new ArgumentNullException(nameof(producer));
            if (consumer == null) throw new ArgumentNullException(nameof(consumer));

            return Task.Run(
                async () =>
                {
                    while (true)
                    {
                        try
                        {
                            var item = await producer(cancellationToken);
                            if (!await consumer(item, cancellationToken))
                            {
                                return item;
                            }
                        }
                        catch (OperationCanceledException) when (handleCancellation && cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                    }

                    return default;
                });
        }
    }

    public static class ProducerExtensions
    {
        /// <summary>
        /// Attach a consumer to the producer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="producer"></param>
        /// <param name="consumer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<T> Consume<T>(this Func<CancellationToken, Task<T>> producer, Func<T, CancellationToken, Task<bool>> consumer, CancellationToken cancellationToken)
        {
            return ProducerConsumer.CreateAsync(producer, consumer, cancellationToken);
        }
    }
}