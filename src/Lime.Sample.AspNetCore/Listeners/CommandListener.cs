using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Transport.AspNetCore;
using Microsoft.Extensions.Logging;

namespace Lime.Sample.AspNetCore.Listeners
{
    public class CommandListener : CommandListenerBase
    {
        private readonly ILogger<CommandListener> _logger;

        public CommandListener(ILogger<CommandListener> logger)
            : base(c => c.Status == CommandStatus.Pending)
        {
            _logger = logger;
        }
        
        public override async Task OnCommandAsync(Command command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Command received");

            var responseCommand = new Command()
            {
                Id = command.Id,
                Method = command.Method,
                Status = CommandStatus.Success
            };

            await Channel.SendCommandAsync(responseCommand, cancellationToken);
        }
    }
}