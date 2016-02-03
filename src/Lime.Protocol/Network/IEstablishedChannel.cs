namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a channel for exchanging envelopes in an established session.
    /// </summary>
    public interface IEstablishedChannel : IMessageChannel, ICommandChannel, INotificationChannel
    {
    }
}