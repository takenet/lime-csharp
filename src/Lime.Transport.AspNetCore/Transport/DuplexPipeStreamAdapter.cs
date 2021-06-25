using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Transport.AspNetCore
{
    internal sealed class DuplexPipeStreamAdapter : Stream
    {
        private readonly Stream _inputStream;
        private readonly Stream _outputStream;

        public DuplexPipeStreamAdapter(IDuplexPipe duplexPipe)
        {
            _inputStream = duplexPipe.Input.AsStream();
            _outputStream = duplexPipe.Output.AsStream();
        }
        
        public override void Flush()
        {
            _outputStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _outputStream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inputStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _inputStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int Read(Span<byte> buffer)
        {
            return _inputStream.Read(buffer);
        }
        
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            return _inputStream.ReadAsync(buffer, cancellationToken);
        }

        public override int ReadByte()
        {
            return _inputStream.ReadByte();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object? state)
        {
            return _inputStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _inputStream.EndRead(asyncResult);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _inputStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _inputStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _outputStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _outputStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            return _outputStream.WriteAsync(buffer, cancellationToken);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _outputStream.Write(buffer);
        }

        public override void WriteByte(byte value)
        {
            _outputStream.WriteByte(value);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object? state)
        {
            return _outputStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _outputStream.EndWrite(asyncResult);
        }

        public override bool CanRead => _inputStream.CanRead;

        public override bool CanSeek => _inputStream.CanSeek;

        public override bool CanWrite => _outputStream.CanWrite;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inputStream.Dispose();
                _outputStream.Dispose();
            }
        }
    }
}