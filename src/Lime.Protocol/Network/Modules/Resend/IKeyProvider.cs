namespace Lime.Protocol.Network.Modules.Resend
{
    /// <summary>
    /// Create the keys to be used by the <see cref="ResendMessagesChannelModule2"/> module.
    /// </summary>
    public interface IKeyProvider
    {
        /// <summary>
        /// Gets a key for the specified channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        string GetChannelKey(IChannel channel);

        /// <summary>
        /// Gets a key for the specified message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        string GetMessageKey(Message message, IChannel channel);

        /// <summary>
        /// Gets a message key for the specified notification.
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        string GetMessageKey(Notification notification, IChannel channel);
    }
}