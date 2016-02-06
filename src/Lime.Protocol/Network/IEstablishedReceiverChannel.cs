namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a channel to receive envelopes in an established session.
    /// </summary>
    public interface IEstablishedReceiverChannel : IMessageReceiverChannel, INotificationReceiverChannel,
        ICommandReceiverChannel
    {

    }
}