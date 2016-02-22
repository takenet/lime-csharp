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
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static Task<T> CreateAsync<T>(Func<CancellationToken, Task<T>> producer, Func<T, Task<bool>> consumer, CancellationToken cancellationToken)
        {
            return CreateAsync(producer, consumer, cancellationToken, TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
        }

        /// <summary>
        /// Creates and starts a task that listens the provided producer and call the consumer until the cancellation is requested.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="producer">The producer func.</param>
        /// <param name="consumer">The consumer func.</param>
        /// <param name="cancellationToken">The cancellation token for the consumer task.</param>
        /// <param name="taskCreationOptions"></param>
        /// <param name="taskScheduler"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static Task<T> CreateAsync<T>(Func<CancellationToken, Task<T>> producer, Func<T, Task<bool>> consumer, CancellationToken cancellationToken, TaskCreationOptions taskCreationOptions, TaskScheduler taskScheduler)
        {
            if (producer == null) throw new ArgumentNullException(nameof(producer));
            if (consumer == null) throw new ArgumentNullException(nameof(consumer));
            if (taskScheduler == null) throw new ArgumentNullException(nameof(taskScheduler));

            return Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var item = await producer(cancellationToken).ConfigureAwait(false);
                    if (!cancellationToken.IsCancellationRequested && !await consumer(item).ConfigureAwait(false))
                    {
                        return item;
                    }
                }
            },
            cancellationToken,
            taskCreationOptions, 
            taskScheduler)
            .Unwrap();
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
        public static Task<T> Consume<T>(this Func<CancellationToken, Task<T>> producer, Func<T, Task<bool>> consumer, CancellationToken cancellationToken)
        {
            return ProducerConsumer.CreateAsync(producer, consumer, cancellationToken);
        }

        /// <summary>
        /// Attach a consumer to the producer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="producer"></param>
        /// <param name="consumer"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="taskCreationOptions"></param>
        /// <param name="taskScheduler"></param>
        /// <returns></returns>
        public static Task<T> Consume<T>(this Func<CancellationToken, Task<T>> producer, Func<T, Task<bool>> consumer, CancellationToken cancellationToken, TaskCreationOptions taskCreationOptions, TaskScheduler taskScheduler)
        {
            return ProducerConsumer.CreateAsync(producer, consumer, cancellationToken, taskCreationOptions,
                taskScheduler);
        }
    }
}
