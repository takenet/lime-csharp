using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Lime.Transport.AspNetCore.Listeners
{
    /// <summary>
    /// Defines a base class for receiving <see cref="Command"/> instances from the connected clients.
    /// </summary>
    public abstract class CommandListenerBase : ICommandListener
    {
        private readonly ISenderChannel? _senderChannel;

        protected CommandListenerBase(ISenderChannel? senderChannel = null)
            : this(_ => true, senderChannel)
        {
        }

        protected CommandListenerBase(Predicate<Command> filter, ISenderChannel? senderChannel = null)
        {
            _senderChannel = senderChannel;
            Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        public Predicate<Command> Filter { get; }

        public async Task OnEnvelopeAsync(Command envelope, CancellationToken cancellationToken)
        {
            try
            {
                await OnCommandAsync(envelope, cancellationToken);
            }
            catch (Exception ex) when (envelope.Id != null &&
                                       envelope.Status == CommandStatus.Pending &&
                                       _senderChannel is { State: SessionState.Established } &&
                                       _senderChannel.Transport.IsConnected)
            {
                await _senderChannel.SendCommandAsync(
                    new Command(envelope.Id)
                    {
                        Method = envelope.Method,
                        Status = CommandStatus.Failure,
                        Reason = ex.ToReason()
                    },
                    default);

                throw;
            }
        }

        public abstract Task OnCommandAsync(Command command, CancellationToken cancellationToken);
    }
}