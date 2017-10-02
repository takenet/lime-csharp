namespace Lime.Protocol.Network.Modules.Resend
{
    public interface IKeyProvider
    {
        string GetChannelKey(IChannel channel);

        string GetMessageKey(Message message, IChannel channel);

        string GetMessageKey(Notification notification, IChannel channel);
    }
}