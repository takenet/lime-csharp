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
        /// Gets the channel created handlers, which are called when a channel is created.
        /// </summary>
        ICollection<Func<ChannelInformation, Task>> ChannelCreatedHandlers { get; }

        /// <summary>
        /// Gets the channel discarded handlers, which are called when a channel is discarded.
        /// </summary>
        ICollection<Func<ChannelInformation, Task>> ChannelDiscardedHandlers { get; }

        /// <summary>
        /// Gets the channel creation failed handlers, which are called when the channel creation failed.
        /// Each handler must return <c>true</c> if the failure was handled and a channel should be created again or <c>false</c> if not, which causes the exception to be thrown to the caller.
        /// The default action is the recreation of a channel. If a single handler return <c>false</c>, no channel will not be recreated.
        /// </summary>
        ICollection<Func<FailedChannelInformation, Task<bool>>> ChannelCreationFailedHandlers { get; }

        /// <summary>
        /// Gets the channel operation failed handlers, which are called when the channel fails during an operation.
        /// Each handler must return <c>true</c> if the failure was handled and a channel should be created again or <c>false</c> if not, which causes the exception to be thrown to the caller.
        /// The default action is the recreation of a channel. If a single handler return <c>false</c>, no channel will not be recreated.
        /// </summary>
        ICollection<Func<FailedChannelInformation, Task<bool>>> ChannelOperationFailedHandlers { get; }

        /// <summary>
        /// Finishes the associated client channel, if established.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task FinishAsync(CancellationToken cancellationToken);
    }
}
