using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Util
{
    /// <summary>
    /// An async-compatible countdown event.
    /// Extracted from: http://nitoasyncex.codeplex.com/SourceControl/latest#Source/Nito.AsyncEx (NET4, Win8, SL4, WP75)/AsyncCountdownEvent.cs
    /// </summary>
    public sealed class AsyncCountdownEvent
    {
        /// <summary>
        /// The TCS used to signal this event.
        /// </summary>
        private readonly TaskCompletionSource<object> _tcs;

        /// <summary>
        /// The remaining count on this event.
        /// </summary>
        private int _count;

        /// <summary>
        /// Creates an async-compatible countdown event.
        /// </summary>
        /// <param name="count">The number of signals this event will need before it becomes set. Must be greater than zero.</param>
        public AsyncCountdownEvent(int count)
        {
            _tcs = new TaskCompletionSource<object>();
            _count = count;
        }

        /// <summary>
        /// Gets a semi-unique identifier for this asynchronous countdown event.
        /// </summary>
        public int Id
        {
            get { return _tcs.Task.Id; }
        }

        /// <summary>
        /// Gets the current number of remaining signals before this event becomes set.
        /// </summary>
        public int CurrentCount
        {
            get
            {
                return Interlocked.CompareExchange(ref _count, 0, 0);
            }
        }

        /// <summary>
        /// Asynchronously waits for this event to be set.
        /// </summary>
        public Task WaitAsync()
        {
            return _tcs.Task;
        }

        /// <summary>
        /// Synchronously waits for this event to be set. This method may block the calling thread.
        /// </summary>
        public void Wait()
        {
            WaitAsync().Wait();
        }

        /// <summary>
        /// Synchronously waits for this event to be set. This method may block the calling thread.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the wait. If this token is already canceled, this method will first check whether the event is set.</param>
        public void Wait(CancellationToken cancellationToken)
        {
            var ret = WaitAsync();
            if (ret.IsCompleted)
                return;
            ret.Wait(cancellationToken);
        }

        /// <summary>
        /// Attempts to modify the current count by the specified amount. This method returns <c>false</c> if the new current count value would be invalid, or if the count has already reached zero.
        /// </summary>
        /// <param name="signalCount">The amount to change the current count. This may be positive or negative, but not zero.</param>
        private bool ModifyCount(int signalCount)
        {
            while (true)
            {
                int oldCount = CurrentCount;
                if (oldCount == 0)
                    return false;
                int newCount = oldCount + signalCount;
                if (newCount < 0)
                    return false;
                if (Interlocked.CompareExchange(ref _count, newCount, oldCount) == oldCount)
                {
                    if (newCount == 0)
                        _tcs.SetResult(null);
                    return true;
                }
            }
        }

        /// <summary>
        /// Attempts to add the specified value to the current count. This method returns <c>false</c> if the count is already at zero or if the new count would be greater than <a href="Int32.MaxValue"/>.
        /// </summary>
        /// <param name="signalCount">The amount to change the current count. This must be greater than zero.</param>
        public bool TryAddCount(int signalCount)
        {
            return ModifyCount(signalCount);
        }

        /// <summary>
        /// Attempts to add one to the current count. This method returns <c>false</c> if the count is already at zero or if the new count would be greater than <a href="Int32.MaxValue"/>.
        /// </summary>
        public bool TryAddCount()
        {
            return TryAddCount(1);
        }

        /// <summary>
        /// Attempts to subtract the specified value from the current count. This method returns <c>false</c> if the count is already at zero or if the new count would be less than zero.
        /// </summary>
        /// <param name="signalCount">The amount to change the current count. This must be greater than zero.</param>
        public bool TrySignal(int signalCount)
        {
            return ModifyCount(-signalCount);
        }

        /// <summary>
        /// Attempts to subtract one from the current count. This method returns <c>false</c> if the count is already at zero or if the new count would be less than zero.
        /// </summary>
        public bool TrySignal()
        {
            return TrySignal(1);
        }

        /// <summary>
        /// Attempts to add the specified value to the current count. This method throws <a href="InvalidOperationException"/> if the count is already at zero or if the new count would be greater than <a href="Int32.MaxValue"/>.
        /// </summary>
        /// <param name="signalCount">The amount to change the current count. This must be greater than zero.</param>
        public void AddCount(int signalCount)
        {
            if (!ModifyCount(signalCount))
                throw new InvalidOperationException("Cannot increment count.");
        }

        /// <summary>
        /// Attempts to add one to the current count. This method throws <a href="InvalidOperationException"/> if the count is already at zero or if the new count would be greater than <a href="Int32.MaxValue"/>.
        /// </summary>
        public void AddCount()
        {
            AddCount(1);
        }

        /// <summary>
        /// Attempts to subtract the specified value from the current count. This method throws <a href="InvalidOperationException"/> if the count is already at zero or if the new count would be less than zero.
        /// </summary>
        /// <param name="signalCount">The amount to change the current count. This must be greater than zero.</param>
        public void Signal(int signalCount)
        {
            if (!ModifyCount(-signalCount))
                throw new InvalidOperationException("Cannot decrement count.");
        }

        /// <summary>
        /// Attempts to subtract one from the current count. This method throws <a href="InvalidOperationException"/> if the count is already at zero or if the new count would be less than zero.
        /// </summary>
        public void Signal()
        {
            Signal(1);
        }       
    }
}
