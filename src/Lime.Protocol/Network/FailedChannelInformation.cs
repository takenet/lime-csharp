using System;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Provides information about a failed channel.
    /// </summary>
    /// <seealso cref="ChannelInformation" />
    public class FailedChannelInformation : ChannelInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FailedChannelInformation"/> class.
        /// </summary>
        /// <param name="sessionId">The identifier.</param>
        /// <param name="state">The state.</param>
        /// <param name="isConnected">if set to <c>true</c> [is connected].</param>
        /// <param name="exception">The exception.</param>
        /// <param name="localNode"></param>
        /// <param name="remoteNode"></param>
        /// <param name="operationName"></param>
        public FailedChannelInformation(
            string sessionId, 
            SessionState state, 
            Node localNode, 
            Node remoteNode, 
            bool isConnected,             
            Exception exception,
            string operationName) 
            : base(sessionId, state, localNode, remoteNode)
        {                        
            IsConnected = isConnected;            
            Exception = exception;
            OperationName = operationName;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Gets the exception that caused the channel to fail.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the name of the operation that failed.
        /// </summary>       
        public string OperationName { get; }
    }
}