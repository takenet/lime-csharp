using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Lime.Transport.AspNetCore.Listeners
{
    /// <summary>
    /// Defines a base class for receiving <see cref="Message"/> instances from the connected clients.
    /// </summary>
    public abstract class MessageListenerBase : IMessageListener
    {
        protected MessageListenerBase()
            : this (_ => true)
        {
            
        }

        protected MessageListenerBase(Predicate<Message> filter)
        {
            Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        public Predicate<Message> Filter { get; }

        public Task OnEnvelopeAsync(Message envelope, CancellationToken cancellationToken) => OnMessageAsync(envelope, cancellationToken);

        public abstract Task OnMessageAsync(Message message, CancellationToken cancellationToken);
    }
}