namespace Lime.Protocol.Network.Modules.Resend
{
    public sealed class KeyProvider : IKeyProvider
    {
        public static KeyProvider Instance { get; } = new KeyProvider();

        public string GetChannelKey(IChannel channel) => $"{channel.RemoteNode}:{channel.LocalNode}".ToLowerInvariant();

        public string GetMessageKey(Message message, IChannel channel) => $"{(message.To ?? channel.RemoteNode).ToIdentity()}:{message.Id}".ToLowerInvariant();

        public string GetMessageKey(Notification notification, IChannel channel) => $"{(notification.From ?? channel.RemoteNode).ToIdentity()}:{notification.Id}".ToLowerInvariant();
    }
}