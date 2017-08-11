using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lime.Protocol.Util
{
    public class SyncStreamWriter
    {
        private readonly Task _writeStreamTask;
        private readonly BufferBlock<byte[]> _writeQueue;
        private readonly Stream _stream;
        private CancellationTokenSource _cancellationTokenSource;

        public Task Execution => _writeStreamTask;
        
        public SyncStreamWriter(Stream stream)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _stream = stream;
            //Increasing this buffer can increase the throuput but comes with a price:
            //The stream can become invalid (disconnection) and the messages will remain in the buffer
            _writeQueue = new BufferBlock<byte[]>(new DataflowBlockOptions { BoundedCapacity = 1 });
            _writeStreamTask = Task.Factory.StartNew(WriteStream, TaskCreationOptions.LongRunning);
        }

        public Task WriteAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            return _writeQueue.SendAsync(bytes, cancellationToken);
        }

        public void Stop()
        {
            _writeQueue.Complete();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        private async Task WriteStream()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var buffer = await _writeQueue.ReceiveAsync(_cancellationTokenSource.Token);
                await _stream.WriteAsync(buffer, 0, buffer.Length);
            }

            while(_writeQueue.Count > 0)
            {
                var buffer = await _writeQueue.ReceiveAsync();
                await _stream.WriteAsync(buffer, 0, buffer.Length);
            }
        }
    }
}
