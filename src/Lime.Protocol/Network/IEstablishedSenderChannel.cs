namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a channel to send envelopes in an established session.
    /// </summary>
    public interface IEstablishedSenderChannel : IMessageSenderChannel, INotificationSenderChannel,
        ICommandSenderChannel
    {
        
    }
}