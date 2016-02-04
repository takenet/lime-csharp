using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;

namespace Lime.Protocol.Adapters
{
    /// <summary>
    /// Base class to build channel listener adapters.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public abstract class ChannelListenerAdapterBase : IDisposable
    {
        private readonly IMessageChannel _messageChannel;
        private readonly INotificationChannel _notificationChannel;
        private readonly ICommandChannel _commandChannel;
        private readonly CancellationTokenSource _cts;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelListenerAdapterBase"/> class.
        /// </summary>
        /// <param name="messageChannel">The message channel.</param>
        /// <param name="notificationChannel">The notification channel.</param>
        /// <param name="commandChannel">The command channel.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        protected ChannelListenerAdapterBase(IMessageChannel messageChannel, INotificationChannel notificationChannel, ICommandChannel commandChannel)
        {
            if (messageChannel == null) throw new ArgumentNullException(nameof(messageChannel));
            if (notificationChannel == null) throw new ArgumentNullException(nameof(notificationChannel));
            if (commandChannel == null) throw new ArgumentNullException(nameof(commandChannel));

            _messageChannel = messageChannel;
            _notificationChannel = notificationChannel;
            _commandChannel = commandChannel;
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the channel listener tasks.
        /// </summary>
        /// <param name="messageConsumer">The message consumer.</param>
        /// <param name="notificationConsumer">The notification consumer.</param>
        /// <param name="commandConsumer">The command consumer.</param>
        protected void StartListenerTasks(Func<Message, Task> messageConsumer, Func<Notification, Task> notificationConsumer, Func<Command, Task> commandConsumer)
        {
            if (messageConsumer == null) throw new ArgumentNullException(nameof(messageConsumer));
            if (notificationConsumer == null) throw new ArgumentNullException(nameof(notificationConsumer));
            if (commandConsumer == null) throw new ArgumentNullException(nameof(commandConsumer));

            MessageListenerTask = ProducerConsumer.StartAsync(_messageChannel.ReceiveMessageAsync,
                messageConsumer, _cts.Token);
            NotificationListenerTask = ProducerConsumer.StartAsync(_notificationChannel.ReceiveNotificationAsync,
                notificationConsumer, _cts.Token);
            CommandListenerTask = ProducerConsumer.StartAsync(_commandChannel.ReceiveCommandAsync,
                commandConsumer, _cts.Token);
        }

        /// <summary>
        /// Gets the message listener task.
        /// </summary>
        public Task MessageListenerTask { get; private set; }

        /// <summary>
        /// Gets the notification listener task.
        /// </summary>
        public Task NotificationListenerTask { get; private set; }

        /// <summary>
        /// Gets the command listener task.
        /// </summary>
        public Task CommandListenerTask { get; private set; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {                                
                _cts.Cancel();
                _cts.Dispose();   
            }
        }
    }
}
