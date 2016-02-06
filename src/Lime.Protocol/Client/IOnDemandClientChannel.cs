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
    /// <seealso cref="IEstablishedChannel" />
    public interface IOnDemandClientChannel : IEstablishedChannel
    {
        /// <summary>
        /// Gets a value indicating whether this instance has an established client channel.
        /// </summary>        
        bool IsEstablished { get; }

        /// <summary>
        /// Occurs when a channel is created.
        /// </summary>
        event EventHandler<ClientChannelEventArgs> ChannelCreated;

        /// <summary>
        /// Occurs when a channel is discarded.
        /// </summary>
        event EventHandler<ClientChannelEventArgs> ChannelDiscarded;

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
        Task FinishAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Provides information about a client channel.
    /// </summary>
    public class ClientChannelEventArgs : DeferralEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientChannelEventArgs"/> class.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="state"></param>
        public ClientChannelEventArgs(Guid id, SessionState state)
        {
            Id = id;
            State = state;
        }

        /// <summary>
        /// Gets the associated channel session identifier.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the channel state.
        /// </summary>
        public SessionState State { get; }
    }
}
