using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Transport.AspNetCore.Listeners;

namespace Lime.Transport.AspNetCore.UnitTests
{
    public class FakeEnvelopeListener<T> : IEnvelopeListener<T> where T : Envelope, new()
    {
        public FakeEnvelopeListener()
        {
            Envelopes = new List<T>();
        }
        
        public List<T> Envelopes { get;  }
        
        public Predicate<T> Filter { get; } = _ => true;
        public Task OnEnvelopeAsync(T envelope, CancellationToken cancellationToken)
        {
            Envelopes.Add(envelope);
            return Task.CompletedTask;
        }
    }
}