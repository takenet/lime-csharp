using Lime.Protocol.Network;

namespace Lime.Transport.AspNetCore
{
    /// <summary>
    /// Defines a channel for sending envelopes.
    /// </summary>
    public interface ISenderChannel : IMessageSenderChannel, INotificationSenderChannel, ICommandSenderChannel, ICommandProcessor, IChannelInformation
    {
        /// <summary>
        /// Gets information about the channel transport.
        /// </summary>
        ITransportInformation Transport { get; }
    }
}