using System.Threading.Tasks;

namespace Lime.Protocol.Listeners
{
    public interface IChanneListener : IStartable, IStoppable
    {
        /// <summary>
        /// Gets the message listener task.
        /// </summary>
        Task MessageListenerTask { get; }

        /// <summary>
        /// Gets the notification listener task.
        /// </summary>
        Task NotificationListenerTask { get; }

        /// <summary>
        /// Gets the command listener task.
        /// </summary>
        Task CommandListenerTask { get; }
    }
}