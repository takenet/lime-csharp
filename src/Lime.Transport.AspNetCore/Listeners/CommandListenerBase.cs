using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Lime.Transport.AspNetCore
{
    public abstract class CommandListenerBase : ICommandListener
    {
        protected CommandListenerBase()
            : this (_ => true)
        {
            
        }
        protected CommandListenerBase(Predicate<Command> filter)
        {
            Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        public Predicate<Command> Filter { get; }

        public Task OnEnvelopeAsync(Command envelope, CancellationToken cancellationToken) => OnCommandAsync(envelope, cancellationToken);

        public abstract Task OnCommandAsync(Command command, CancellationToken cancellationToken);
    }
}