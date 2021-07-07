using System;
using System.Collections.Generic;
using System.Linq;
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
            SentEnvelopes = new List<Envelope>();
            ReceiveBuffer = new BufferBlock<Envelope>(
                new DataflowBlockOptions()
                {
                    BoundedCapacity = -1
                });
            SentHandlers = new List<Func<Envelope, Task>>();
        }

        public List<Envelope> SentEnvelopes { get; }

        public BufferBlock<Envelope> ReceiveBuffer { get; }

        public ICollection<Func<Envelope, Task>> SentHandlers { get; }

        public Func<Task> ReceivingHandler { get; set; }
        
        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            SentEnvelopes.Add(envelope);
            await Task.WhenAll(SentHandlers.Select(h => h.Invoke(envelope)));
        }

        public override async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            await (ReceivingHandler?.Invoke() ?? Task.CompletedTask);
            return await ReceiveBuffer.ReceiveAsync(cancellationToken);
        }

        public override bool IsConnected => !ReceiveBuffer.Completion.IsCompleted;

        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            ReceiveBuffer.Complete();
            return Task.CompletedTask;
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}