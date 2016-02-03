namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a channel for exchanging envelopes while established.
    /// </summary>
    public interface IEstablishedChannel : IMessageChannel, ICommandChannel, INotificationChannel
    {
    }
}