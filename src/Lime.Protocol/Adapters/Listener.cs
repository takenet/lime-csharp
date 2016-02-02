using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Adapters
{
    public static class Listener
    {
        /// <summary>
        /// Creates a long running task that listens the provided listener and call the consumer until the cancellation is requested.
        /// The token cancellation occurs silently.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listener">The listener func.</param>
        /// <param name="consumer">The consumer func.</param>
        /// <param name="cancellationToken">The cancellation token to stop the listener. It must be a valid token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        /// <exception cref="System.ArgumentException">A valid cancellation token must be provided</exception>
        public static Task ListenAsync<T>(Func<CancellationToken, Task<T>> listener, Func<T, Task> consumer, CancellationToken cancellationToken)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (consumer == null) throw new ArgumentNullException(nameof(consumer));            
            if (cancellationToken == CancellationToken.None) throw new ArgumentException("A valid cancellation token must be provided", nameof(cancellationToken));
            return Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var item = await listener(cancellationToken);
                        await consumer(item);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            },            
            TaskCreationOptions.LongRunning);
        }
    }
}
