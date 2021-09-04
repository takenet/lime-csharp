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
        private readonly ISenderChannel? _senderChannel;

        
        protected MessageListenerBase(ISenderChannel? senderChannel = null)
            : this (_ => true, senderChannel)
        {
            
        }

        protected MessageListenerBase(Predicate<Message> filter, ISenderChannel? senderChannel = null)
        {
            Filter = filter ?? throw new ArgumentNullException(nameof(filter));
            _senderChannel = senderChannel;
        }

        public Predicate<Message> Filter { get; }

        public async Task OnEnvelopeAsync(Message envelope, CancellationToken cancellationToken)
        {
            try
            {
                await OnMessageAsync(envelope, cancellationToken);
            }
            catch (Exception ex) when (envelope.Id != null &&
                                       _senderChannel is { State: SessionState.Established } &&
                                       _senderChannel.Transport.IsConnected)
            {
                await _senderChannel.SendNotificationAsync(
                    new Notification(envelope.Id)
                    {
                        Event = Event.Failed,
                        Reason = ex.ToReason()
                    },
                    default);

                throw;
            }
        }

        public abstract Task OnMessageAsync(Message message, CancellationToken cancellationToken);
    }
}