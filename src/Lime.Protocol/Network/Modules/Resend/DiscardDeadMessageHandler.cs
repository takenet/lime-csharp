using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Util;

namespace Lime.Protocol.Network.Modules.Resend
{
    public sealed class DiscardDeadMessageHandler : IDeadMessageHandler
    {
        public static DiscardDeadMessageHandler Instance { get; } = new DiscardDeadMessageHandler();

        public Task HandleDeadMessageAsync(Message message, IChannelInformation channelInformation,
            CancellationToken cancellationToken)
            => TaskUtil.CompletedTask;
    }
}