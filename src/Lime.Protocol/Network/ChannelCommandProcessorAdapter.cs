using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Adapter to use an <see cref="ICommandProcessor"/> as an <see cref="IChannelCommandProcessor"/>.
    /// </summary>
    public sealed class ChannelCommandProcessorAdapter : IChannelCommandProcessor
    {
        private readonly ICommandProcessor _commandProcessor;

        public ChannelCommandProcessorAdapter(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor ?? throw new ArgumentNullException(nameof(commandProcessor));
        }

        public Task<Command> ProcessCommandAsync(Command requestCommand, CancellationToken cancellationToken) 
            => _commandProcessor.ProcessCommandAsync(requestCommand, cancellationToken);

        public bool TrySubmitCommandResult(Command responseCommand) => false;

        public void CancelAll()
        {
            
        }
    }
}