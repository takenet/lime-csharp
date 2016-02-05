using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;

namespace Lime.Protocol.Listeners
{
    /// <summary>
    /// Creates listener loop tasks that receive envelopes from the channel and calls a consumer delegate.
    /// A task is created for each envelope type.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public sealed class ChannelListener : IChanneListener, IDisposable
    {
        private readonly IMessageChannel _messageChannel;
        private readonly INotificationChannel _notificationChannel;
        private readonly ICommandChannel _commandChannel;
        private readonly Func<Message, Task<bool>> _messageConsumer;
        private readonly Func<Notification, Task<bool>> _notificationConsumer;
        private readonly Func<Command, Task<bool>> _commandConsumer;
        private readonly CancellationTokenSource _cts;
        private readonly object _syncRoot;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelListener"/> class.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="messageConsumer">The message consumer.</param>
        /// <param name="notificationConsumer">The notification consumer.</param>
        /// <param name="commandConsumer">The command consumer.</param>
        public ChannelListener(IEstablishedChannel channel,
            Func<Message, Task<bool>> messageConsumer, Func<Notification, Task<bool>> notificationConsumer,
            Func<Command, Task<bool>> commandConsumer)
            : this(channel, channel, channel, messageConsumer, notificationConsumer, commandConsumer)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelListener"/> class.
        /// </summary>
        /// <param name="messageChannel">The message channel.</param>
        /// <param name="notificationChannel">The notification channel.</param>
        /// <param name="commandChannel">The command channel.</param>
        /// <param name="messageConsumer">The message consumer.</param>
        /// <param name="notificationConsumer">The notification consumer.</param>
        /// <param name="commandConsumer">The command consumer.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public ChannelListener(IMessageChannel messageChannel, INotificationChannel notificationChannel, ICommandChannel commandChannel,
            Func<Message, Task<bool>> messageConsumer, Func<Notification, Task<bool>> notificationConsumer, Func<Command, Task<bool>> commandConsumer)
        {
            if (messageChannel == null) throw new ArgumentNullException(nameof(messageChannel));
            if (notificationChannel == null) throw new ArgumentNullException(nameof(notificationChannel));
            if (commandChannel == null) throw new ArgumentNullException(nameof(commandChannel));
            if (messageConsumer == null) throw new ArgumentNullException(nameof(messageConsumer));
            if (notificationConsumer == null) throw new ArgumentNullException(nameof(notificationConsumer));
            if (commandConsumer == null) throw new ArgumentNullException(nameof(commandConsumer));

            _messageChannel = messageChannel;
            _notificationChannel = notificationChannel;
            _commandChannel = commandChannel;
            _messageConsumer = messageConsumer;
            _notificationConsumer = notificationConsumer;
            _commandConsumer = commandConsumer;
            _cts = new CancellationTokenSource();
            _syncRoot = new object();
        }

        /// <summary>
        /// Starts the channel listener tasks.
        /// </summary>

        public void Start()
        {
            lock (_syncRoot)
            {
                if (MessageListenerTask == null)
                {
                    MessageListenerTask = ProducerConsumer.CreateAsync(_messageChannel.ReceiveMessageAsync,
                        _messageConsumer, _cts.Token);
                }

                if (NotificationListenerTask == null)
                {
                    NotificationListenerTask = ProducerConsumer.CreateAsync(_notificationChannel.ReceiveNotificationAsync,
                        _notificationConsumer, _cts.Token);
                }

                if (CommandListenerTask == null)
                {
                    CommandListenerTask = ProducerConsumer.CreateAsync(_commandChannel.ReceiveCommandAsync,
                        _commandConsumer, _cts.Token);
                }
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            lock (_syncRoot)
            {
                if (!_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }
            }
        }

        /// <summary>
        /// Gets the message listener task.
        /// </summary>
        public Task<Message> MessageListenerTask { get; private set; }

        /// <summary>
        /// Gets the notification listener task.
        /// </summary>
        public Task<Notification> NotificationListenerTask { get; private set; }

        /// <summary>
        /// Gets the command listener task.
        /// </summary>
        public Task<Command> CommandListenerTask { get; private set; }

        /// <summary>
        /// Stops the listener tasks and releases any related resource.
        /// </summary>
        public void Dispose()
        {
            if (!_cts.IsCancellationRequested) _cts.Cancel();
            _cts.Dispose();
            _messageChannel.DisposeIfDisposable();
            if (!ReferenceEquals(_messageChannel, _notificationChannel))
            {
                _notificationConsumer.DisposeIfDisposable();
            }
            if (!ReferenceEquals(_messageChannel, _commandChannel) &&
                !ReferenceEquals(_notificationChannel, _commandChannel))
            {
                _commandChannel.DisposeIfDisposable();
            }
        }
    }
}
