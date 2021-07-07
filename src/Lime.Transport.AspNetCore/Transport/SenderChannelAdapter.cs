using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Network;

namespace Lime.Transport.AspNetCore.Transport
{
    public sealed class SenderChannelAdapter : ISenderChannel
    {
        private readonly IChannel _channel;

        public SenderChannelAdapter(IChannel channel)
        {
            _channel = channel;
            Transport = new TransportInformation(
                _channel.Transport.Compression,
                _channel.Transport.Encryption,
                _channel.Transport.IsConnected,
                _channel.Transport.LocalEndPoint,
                _channel.Transport.RemoteEndPoint,
                _channel.Transport.Options);
        }
        
        public string SessionId => _channel.SessionId;

        public SessionState State => _channel.State;

        public Node LocalNode => _channel.LocalNode;

        public Node RemoteNode => _channel.RemoteNode;

        public ITransportInformation Transport { get; }

        public Task SendMessageAsync(Message message, CancellationToken cancellationToken) => _channel.SendMessageAsync(message, cancellationToken);

        public Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken) => _channel.SendNotificationAsync(notification, cancellationToken);

        public Task SendCommandAsync(Command command, CancellationToken cancellationToken) => _channel.SendCommandAsync(command, cancellationToken);

        public Task<Command> ProcessCommandAsync(Command requestCommand, CancellationToken cancellationToken) => _channel.ProcessCommandAsync(requestCommand, cancellationToken);
    }
}