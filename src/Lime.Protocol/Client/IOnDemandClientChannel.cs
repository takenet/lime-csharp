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
    /// <seealso cref="Lime.Protocol.Network.ICommandChannel" />
    /// <seealso cref="Lime.Protocol.Network.IMessageChannel" />
    /// <seealso cref="Lime.Protocol.Network.INotificationChannel" />
    public interface IOnDemandClientChannel : ICommandChannel, IMessageChannel, INotificationChannel
    {
        /// <summary>
        /// Occurs when the channel creation failed.
        /// </summary>
        event EventHandler<ExceptionEventArgs> ChannelCreationFailed;

        /// <summary>
        /// Occurs when the channel send or receive action failed.
        /// </summary>
        event EventHandler<ExceptionEventArgs> ChannelOperationFailed;

        /// <summary>
        /// Finishes the associated client channel, if available.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task FinishClientChannelAsync(CancellationToken cancellationToken);
    }
}
