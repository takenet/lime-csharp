using System.Threading.Tasks;
using Lime.Protocol.Network;

namespace Lime.Protocol.Listeners
{
    /// <summary>
    /// Defines a channel listener service.
    /// </summary>    
    public interface IChannelListener
    {
        /// <summary>
        /// Gets the message listener task. 
        /// When completed, return the last unconsumed <see cref="Message"/>, if there's any.
        /// </summary>
        Task<Message> MessageListenerTask { get; }

        /// <summary>
        /// Gets the notification listener task.
        /// When completed, return the last unconsumed <see cref="Notification"/>, if there's any.
        /// </summary>
        Task<Notification> NotificationListenerTask { get; }

        /// <summary>
        /// Gets the command listener task.
        /// When completed, return the last unconsumed <see cref="Command"/>, if there's any.
        /// </summary>
        Task<Command> CommandListenerTask { get; }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <param name="channel">The channel to be listened.</param>
        void Start(IEstablishedReceiverChannel channel);

        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop();
    }
}