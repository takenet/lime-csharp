namespace Lime.Protocol.Network.Modules.Resend
{
    public sealed class KeyProvider : IKeyProvider
    {
        public string GetChannelKey(IChannel channel) => $"{channel.RemoteNode.ToIdentity()}:{channel.LocalNode.ToIdentity()}".ToLowerInvariant();

        public string GetMessageKey(Message message, IChannel channel) => $"{(message.To ?? channel.RemoteNode).ToIdentity()}:{message.Id}".ToLowerInvariant();

        public string GetMessageKey(Notification notification, IChannel channel) => $"{(notification.From ?? channel.RemoteNode).ToIdentity()}:{notification.Id}".ToLowerInvariant();
    }
}