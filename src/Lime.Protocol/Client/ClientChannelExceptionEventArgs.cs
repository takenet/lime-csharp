using System;
using Lime.Protocol.Network;

namespace Lime.Protocol.Client
{
    /// <summary>
    /// Provides information about an exception that occurred in a <see cref="ClientChannel"/> instance.
    /// </summary>
    /// <seealso cref="ExceptionEventArgs" />
    public class ClientChannelExceptionEventArgs : ExceptionEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientChannelExceptionEventArgs"/> class.
        /// </summary>
        /// <param name="isHandled">if set to <c>true</c> [is reconnecting].</param>
        /// <param name="exception">The exception.</param>
        public ClientChannelExceptionEventArgs(bool isHandled, Exception exception)
            : this(Guid.Empty, SessionState.Failed, false, isHandled, exception)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientChannelExceptionEventArgs"/> class.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="isConnected">if set to <c>true</c> [is reconnecting].</param>
        /// <param name="isHandled"></param>
        /// <param name="exception">The exception.</param>
        /// <param name="sessionId"></param>
        public ClientChannelExceptionEventArgs(Guid sessionId, SessionState state, bool isConnected, bool isHandled, Exception exception) 
            : base(exception)
        {
            SessionId = sessionId;
            State = state;
            IsConnected = isConnected;
            IsHandled = isHandled;
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
        /// Gets or sets a value indicating whether the exception is handled.
        /// If false, the exception will be thrown to the channel.
        /// </summary>
        public bool IsHandled { get; set; }
    }
}