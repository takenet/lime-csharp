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
        private readonly ChannelContext _channelContext;

        public CommandListener(ILogger<CommandListener> logger, ChannelContext channelContext)
            : base(c => c.Status == CommandStatus.Pending)
        {
            _logger = logger;
            _channelContext = channelContext;
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

            await _channelContext.Channel.SendCommandAsync(responseCommand, cancellationToken);
        }
    }
}