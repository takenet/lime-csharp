namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a channel to exchange envelopes in an established session.
    /// </summary>
    public interface IEstablishedChannel : IEstablishedSenderChannel, IEstablishedReceiverChannel, IMessageChannel, INotificationChannel, ICommandChannel
    {

    }
}