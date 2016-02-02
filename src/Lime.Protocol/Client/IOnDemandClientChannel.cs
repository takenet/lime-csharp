using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;

namespace Lime.Protocol.Client
{
    /// <summary>
    /// Defines a client channel that manages the session state and connects to the server on demand.
    /// </summary>
    /// <seealso cref="ICommandChannel" />
    /// <seealso cref="IMessageChannel" />
    /// <seealso cref="INotificationChannel" />
    public interface IOnDemandClientChannel : ICommandChannel, IMessageChannel, INotificationChannel
    {
        /// <summary>
        /// Gets a value indicating whether this instance has an established client channel.
        /// </summary>        
        bool IsEstablished { get; }

        /// <summary>
        /// Occurs when the channel creation failed.
        /// </summary>
        event EventHandler<ClientChannelExceptionEventArgs> ChannelCreationFailed;

        /// <summary>
        /// Occurs when the channel send or receive action failed.
        /// </summary>
        event EventHandler<ClientChannelExceptionEventArgs> ChannelOperationFailed;

        /// <summary>
        /// Finishes the associated client channel, if established.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task FinishClientChannelAsync(CancellationToken cancellationToken);
    }
}
