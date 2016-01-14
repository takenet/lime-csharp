using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lime.Protocol.Util
{
    /// <summary>
    /// Adapts the IAsyncQueue interface
    /// to the TPL Dataflow BufferBlock.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BufferBlockAsyncQueue<T> : IAsyncQueue<T>
    {
        #region Constructor

        public BufferBlockAsyncQueue()
            : this(DataflowBlockOptions.Unbounded)
        {

        }

        public BufferBlockAsyncQueue(int boundedCapacity)
        {
            var dataflowBlockOptions = new DataflowBlockOptions()
            {
                BoundedCapacity = boundedCapacity
            };

            BufferBlock = new BufferBlock<T>(dataflowBlockOptions);
        }

        #endregion

        #region IAsyncQueue<T> Members

        public bool Post(T item)
        {
            return BufferBlock.Post(item);
        }

        public bool TryTake(out T item)
        {
            return BufferBlock.TryReceive(out item);
        }

        public Task<bool> SendAsync(T item, CancellationToken cancellationToken)
        {
            return BufferBlock.SendAsync(item, cancellationToken);
        }

        public Task<T> ReceiveAsync(CancellationToken cancellationToken)
        {
            return BufferBlock.ReceiveAsync(cancellationToken);
        }

        #endregion

        public BufferBlock<T> BufferBlock { get; }
    }
}