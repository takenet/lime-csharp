using System;
using System.Collections.Generic;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a communication channel for the protocol.
    /// </summary>
    /// <seealso cref="IEstablishedChannel" />
    /// <seealso cref="ISessionChannel" />
    public interface IChannel : ISenderChannel, IReceiverChannel, IEstablishedChannel, ISessionChannel, IChannelInformation, IFlushable
    {
        /// <summary>
        /// Gets the current session transport.
        /// </summary>
        ITransport Transport { get; }

        /// <summary>
        /// Gets the message modules for processing sent and received messages.
        /// </summary>
        ICollection<IChannelModule<Message>> MessageModules { get; }

        /// <summary>
        /// Gets the notification modules for processing sent and received notifications.
        /// </summary>
        ICollection<IChannelModule<Notification>> NotificationModules { get; }

        /// <summary>
        /// Gets the command modules for processing sent and received commands.
        /// </summary>
        ICollection<IChannelModule<Command>> CommandModules { get; }

        /// <summary>
        /// Occurs when the channel consumer task throws an exception.
        /// </summary>
        event EventHandler<ExceptionEventArgs> ConsumerException;

        /// <summary>
        /// Occurs when the channel sender task throws an exception.
        /// </summary>
        event EventHandler<ExceptionEventArgs> SenderException;
    }
}
