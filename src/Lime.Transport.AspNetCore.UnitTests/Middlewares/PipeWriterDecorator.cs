using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    public class PipeWriterDecorator : PipeWriter
    {
        private readonly PipeWriter _writer;

        public PipeWriterDecorator(PipeWriter writer)
        {
            _writer = writer;
        }

        public int FlushCount;
        public bool CompleteCalled;

        public override void Advance(int bytes)
        {
            _writer.Advance(bytes);
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            return _writer.GetMemory(sizeHint);
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            return _writer.GetSpan(sizeHint);
        }

        public override void CancelPendingFlush()
        {
            _writer.CancelPendingFlush();
        }

        public override void Complete(Exception exception = null)
        {
            CompleteCalled = true;
            _writer.Complete(exception);
        }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            FlushCount++;
            return _writer.FlushAsync(cancellationToken);
        }
    }
}