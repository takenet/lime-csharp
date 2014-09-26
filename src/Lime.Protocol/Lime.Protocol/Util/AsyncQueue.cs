using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Util
{
    public class AsyncQueue<T> : IAsyncQueue<T>
    {
        #region Private fields

        private ConcurrentQueue<T> _bufferQueue;
        private ConcurrentQueue<TaskCompletionSource<T>> _promisesQueue;
        private object _syncRoot = new object();
        private int _promisesLimit;
        private int _bufferLimit;

        #endregion

        #region Constructor

        public AsyncQueue()
            : this(0, 0)
        {

        }

        public AsyncQueue(int promisesLimit, int bufferLimit)
        {
            _promisesLimit = promisesLimit;
            _bufferLimit = bufferLimit;

            _bufferQueue = new ConcurrentQueue<T>();
            _promisesQueue = new ConcurrentQueue<TaskCompletionSource<T>>();
        }

        #endregion

        #region IAsyncQueue<T> Members

        /// <summary>
        /// Enqueues the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
		public bool Post(T item)
        {
            TaskCompletionSource<T> promise;
            do
            {
                if (_promisesQueue.TryDequeue(out promise) &&
                    !promise.Task.IsCanceled &&
                    promise.TrySetResult(item))
                {
					return true;                                       
                }
            }
            while (promise != null);

            lock (_syncRoot)
            {
                if (_promisesQueue.TryDequeue(out promise) &&
                    !promise.Task.IsCanceled &&
                    promise.TrySetResult(item))
                {
					return true;
                }

                if (_bufferLimit != 0 &&
                    this.BufferCount >= _bufferLimit)
                {
					return false;
                }

                _bufferQueue.Enqueue(item);
				return true;
            }            
        }

        public Task SendAsync(T item, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Dequeues the asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
		public Task<T> ReceiveAsync(CancellationToken cancellationToken)
        {
            T item;            

            if (!_bufferQueue.TryDequeue(out item))
            {
                lock (_syncRoot)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!_bufferQueue.TryDequeue(out item))
                    {
                        if (_promisesLimit != 0 &&
                            this.PromisesCount >= _promisesLimit)
                        {
                            throw new InvalidOperationException("Queue promises limit reached");
                        }

                        var promise = new TaskCompletionSource<T>();
                        cancellationToken.Register(() => promise.TrySetCanceled());

                        _promisesQueue.Enqueue(promise);

                        return promise.Task;
                    }
                }
            }

            return Task.FromResult(item);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets a value indicating whether this instance has promises.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has promises; otherwise, <c>false</c>.
        /// </value>
        internal bool HasPromises
        {
            get { return this.PromisesCount > 0; }
        }

        /// <summary>
        /// Gets the total of enqueued items
        /// in the promises queue.
        /// </summary>
        internal int PromisesCount
        {
            get { return _promisesQueue.Where(p => !p.Task.IsCanceled).Count(); }
        }

        /// <summary>
        /// Gets the total of enqueued items
        /// in the buffer queue.
        /// </summary>
        internal int BufferCount
        {
            get { return _bufferQueue.Count; }
        }

        #endregion
    }
}