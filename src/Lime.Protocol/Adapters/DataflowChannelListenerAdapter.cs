using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Client;
using Lime.Protocol.Network;

namespace Lime.Protocol.Adapters
{
    /// <summary>
    /// Listens to a <see cref="IChannel"/> instance and pushes the received envelopes to a <see cref="ITargetBlock{T}"/>.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Adapters.ChannelListenerAdapterBase" />
    public sealed class DataflowChannelListenerAdapter : ChannelListenerAdapterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataflowChannelListenerAdapter" /> class.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="messageTargetBlock">The message target block.</param>
        /// <param name="notificationTargetBlock">The notification target block.</param>
        /// <param name="commandTargetBlock">The command target block.</param>
        public DataflowChannelListenerAdapter(IChannel channel, ITargetBlock<Message> messageTargetBlock, ITargetBlock<Notification> notificationTargetBlock, ITargetBlock<Command> commandTargetBlock)
            : this(channel, channel, channel, messageTargetBlock, notificationTargetBlock, commandTargetBlock)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataflowChannelListenerAdapter" /> class.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="messageTargetBlock">The message target block.</param>
        /// <param name="notificationTargetBlock">The notification target block.</param>
        /// <param name="commandTargetBlock">The command target block.</param>
        public DataflowChannelListenerAdapter(IOnDemandClientChannel channel, ITargetBlock<Message> messageTargetBlock, ITargetBlock<Notification> notificationTargetBlock, ITargetBlock<Command> commandTargetBlock)
            : this(channel, channel, channel, messageTargetBlock, notificationTargetBlock, commandTargetBlock)
        {

        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DataflowChannelListenerAdapter" /> class.
        /// </summary>
        /// <param name="messageChannel">The message channel.</param>
        /// <param name="notificationChannel">The notification channel.</param>
        /// <param name="commandChannel">The command channel.</param>
        /// <param name="messageTargetBlock">The message target block.</param>
        /// <param name="notificationTargetBlock">The notification target block.</param>
        /// <param name="commandTargetBlock">The command target block.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public DataflowChannelListenerAdapter(IMessageChannel messageChannel, INotificationChannel notificationChannel, ICommandChannel commandChannel, 
            ITargetBlock<Message> messageTargetBlock, ITargetBlock<Notification> notificationTargetBlock, ITargetBlock<Command> commandTargetBlock)
            : base(messageChannel, notificationChannel, commandChannel)

        {
            if (messageTargetBlock == null) throw new ArgumentNullException(nameof(messageTargetBlock));
            if (notificationTargetBlock == null) throw new ArgumentNullException(nameof(notificationTargetBlock));
            if (commandTargetBlock == null) throw new ArgumentNullException(nameof(commandTargetBlock));
            StartListenerTasks(messageTargetBlock.SendAsync, notificationTargetBlock.SendAsync,
                commandTargetBlock.SendAsync);
        }
    }
}
