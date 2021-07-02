using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol;
using Lime.Protocol.Network;

namespace Lime.Transport.AspNetCore.UnitTests.Transport
{
    public class FakeTransport : TransportBase, ITransport
    {                
        public FakeTransport()
        {
            SendBuffer = new BufferBlock<Envelope>(
                new DataflowBlockOptions()
                {
                    BoundedCapacity = -1
                });
            ReceiveBuffer = new BufferBlock<Envelope>(
                new DataflowBlockOptions()
                {
                    BoundedCapacity = -1
                });
        }

        public BufferBlock<Envelope> SendBuffer { get; }

        public BufferBlock<Envelope> ReceiveBuffer { get; set; }

        public override Task SendAsync(Envelope envelope, CancellationToken cancellationToken) => SendBuffer.SendAsync(envelope, cancellationToken);

        public override Task<Envelope> ReceiveAsync(CancellationToken cancellationToken) => ReceiveBuffer.ReceiveAsync(cancellationToken);

        public override bool IsConnected => !SendBuffer.Completion.IsCompleted;

        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            SendBuffer.Complete();
            return Task.CompletedTask;
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}