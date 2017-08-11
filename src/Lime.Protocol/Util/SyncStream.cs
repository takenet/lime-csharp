using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lime.Protocol.Util
{
    public class SyncStream : Stream
    {
        private readonly Stream _stream;
        private readonly Task _writeStreamTask;
        private readonly BufferBlock<byte[]> _writeQueue;
        private CancellationTokenSource _cancellationTokenSource;

        public SyncStream(Stream stream)
        {
            _stream = stream;
            _cancellationTokenSource = new CancellationTokenSource();
            //Increasing this buffer can increase the throuput but comes with a price:
            //The stream can become invalid (disconnection) and the messages will remain in the buffer
            _writeQueue = new BufferBlock<byte[]>(new DataflowBlockOptions { BoundedCapacity = 1 });
            _writeStreamTask = Task.Factory.StartNew(WriteStream, TaskCreationOptions.LongRunning);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _writeQueue.SendAsync(buffer, cancellationToken);
        }

        private async Task WriteStream()
        {
            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var buffer = await _writeQueue.ReceiveAsync(_cancellationTokenSource.Token);
                    await _stream.WriteAsync(buffer, 0, buffer.Length);
                }

                while (_writeQueue.Count > 0)
                {
                    var buffer = await _writeQueue.ReceiveAsync();
                    await _stream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
            catch (Exception e)
            {
                var dump = e.ToString();
            }
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position { get => _stream.Position; set => _stream.Position = value; }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _stream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override bool Equals(object obj)
        {
            return _stream.Equals(obj);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _stream.FlushAsync(cancellationToken);
        }

        public override int GetHashCode()
        {
            return _stream.GetHashCode();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override bool CanTimeout => _stream.CanTimeout;

        public override int ReadByte()
        {
            return _stream.ReadByte();
        }

        public override int ReadTimeout { get => _stream.ReadTimeout; set => _stream.ReadTimeout = value; }

        public override string ToString()
        {
            return _stream.ToString();
        }

        public override void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }

        public override int WriteTimeout { get => _stream.WriteTimeout; set => _stream.WriteTimeout = value; }
        
        public void Dispose()
        {
            _writeQueue.Complete();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _stream.Dispose();
            base.Dispose();
        }
    }
}
