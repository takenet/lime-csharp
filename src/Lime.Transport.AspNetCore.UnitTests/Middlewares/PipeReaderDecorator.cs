using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    public class PipeReaderDecorator : PipeReader
    {
        private readonly PipeReader _reader;

        public int ReadCount;
        public bool CompleteCalled;
        public Action ReadCalledHandler;
        
        public PipeReaderDecorator(PipeReader reader)
        {
            _reader = reader;
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            _reader.AdvanceTo(consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            _reader.AdvanceTo(consumed, examined);
        }

        public override void CancelPendingRead()
        {
            _reader.CancelPendingRead();
        }

        public override void Complete(Exception exception = null)
        {
            CompleteCalled = true;
            _reader.Complete(exception);
        }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            ReadCount++;
            ReadCalledHandler?.Invoke();
            return _reader.ReadAsync(cancellationToken);
        }

        public override bool TryRead(out ReadResult result)
        {
            ReadCount++;
            ReadCalledHandler?.Invoke();
            return _reader.TryRead(out result);
        }
    }
}