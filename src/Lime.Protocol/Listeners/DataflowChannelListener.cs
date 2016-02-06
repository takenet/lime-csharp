using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Network;

namespace Lime.Protocol.Listeners
{
    /// <summary>
    /// Listens to a <see cref="IChannel"/> instance and pushes the received envelopes to a <see cref="ITargetBlock{T}"/>.
    /// </summary>
    /// <seealso cref="ChannelListener" />
    public sealed class DataflowChannelListener : IChanneListener, IDisposable
    {
        private readonly ChannelListener _channelListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataflowChannelListener" /> class.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="messageTargetBlock">The message target block.</param>
        /// <param name="notificationTargetBlock">The notification target block.</param>
        /// <param name="commandTargetBlock">The command target block.</param>
        public DataflowChannelListener(IEstablishedReceiverChannel channel, ITargetBlock<Message> messageTargetBlock, ITargetBlock<Notification> notificationTargetBlock, ITargetBlock<Command> commandTargetBlock)
            : this(channel, channel, channel, messageTargetBlock, notificationTargetBlock, commandTargetBlock)
        {           
            
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DataflowChannelListener" /> class.
        /// </summary>
        /// <param name="messageChannel">The message channel.</param>
        /// <param name="notificationChannel">The notification channel.</param>
        /// <param name="commandChannel">The command channel.</param>
        /// <param name="messageTargetBlock">The message target block.</param>
        /// <param name="notificationTargetBlock">The notification target block.</param>
        /// <param name="commandTargetBlock">The command target block.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public DataflowChannelListener(IMessageReceiverChannel messageChannel, INotificationReceiverChannel notificationChannel, ICommandReceiverChannel commandChannel, 
            ITargetBlock<Message> messageTargetBlock, ITargetBlock<Notification> notificationTargetBlock, ITargetBlock<Command> commandTargetBlock)            
        {
            if (messageTargetBlock == null) throw new ArgumentNullException(nameof(messageTargetBlock));
            if (notificationTargetBlock == null) throw new ArgumentNullException(nameof(notificationTargetBlock));
            if (commandTargetBlock == null) throw new ArgumentNullException(nameof(commandTargetBlock));

            _channelListener = new ChannelListener(
                messageChannel, notificationChannel, commandChannel,
                messageTargetBlock.SendAsync,
                notificationTargetBlock.SendAsync,
                commandTargetBlock.SendAsync);
        }

        public void Start()
        {
            _channelListener.Start();
        }

        public void Stop()
        {
            _channelListener.Stop();
        }

        public Task<Message> MessageListenerTask => _channelListener.MessageListenerTask;

        public Task<Notification> NotificationListenerTask => _channelListener.NotificationListenerTask;

        public Task<Command> CommandListenerTask => _channelListener.CommandListenerTask;

        public void Dispose()
        {
            _channelListener.Dispose();
        }
    }
}
