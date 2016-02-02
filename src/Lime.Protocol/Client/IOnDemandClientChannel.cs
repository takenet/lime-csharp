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
        event EventHandler<ClientChannelEventArgs> ChannelCreationFailed;

        /// <summary>
        /// Occurs when the channel send or receive action failed.
        /// </summary>
        event EventHandler<ClientChannelEventArgs> ChannelOperationFailed;

        /// <summary>
        /// Finishes the associated client channel, if available.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task FinishClientChannelAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents event values associate to a channel.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Network.ExceptionEventArgs" />
    public class ClientChannelEventArgs : ExceptionEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientChannelEventArgs"/> class.
        /// </summary>
        /// <param name="isReconnecting">if set to <c>true</c> [is reconnecting].</param>
        /// <param name="exception">The exception.</param>
        public ClientChannelEventArgs(bool isReconnecting, Exception exception)
            : this(Guid.Empty, SessionState.Failed, false, isReconnecting, exception)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientChannelEventArgs"/> class.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="isConnected">if set to <c>true</c> [is reconnecting].</param>
        /// <param name="isReconnecting"></param>
        /// <param name="exception">The exception.</param>
        /// <param name="sessionId"></param>
        public ClientChannelEventArgs(Guid sessionId, SessionState state, bool isConnected, bool isReconnecting, Exception exception) 
            : base(exception)
        {
            SessionId = sessionId;
            State = state;
            IsConnected = isConnected;
            IsReconnecting = isReconnecting;
        }

        /// <summary>
        /// Gets the associated channel session identifier.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Gets the channel state.
        /// </summary>
        public SessionState State { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is reconnecting.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is reconnecting; otherwise, <c>false</c>.
        /// </value>
        public bool IsReconnecting { get; set; }
    }

}
