#if !MONO
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
    public sealed class BufferBlockAsyncQueue<T> : IAsyncQueue<T>
    {
        private readonly BufferBlock<T> _bufferBlock;

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

            _bufferBlock = new BufferBlock<T>(dataflowBlockOptions);
        }

        #endregion

        #region IAsyncQueue<T> Members

        public bool Post(T item)
        {
            return _bufferBlock.Post(item);
        }

        public Task<bool> SendAsync(T item, CancellationToken cancellationToken)
        {
            return _bufferBlock.SendAsync(item, cancellationToken);
        }

        public Task<T> ReceiveAsync(CancellationToken cancellationToken)
        {
            return _bufferBlock.ReceiveAsync(cancellationToken);
        }

        #endregion
    }
}
#endif