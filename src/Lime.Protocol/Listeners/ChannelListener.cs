using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Util;

namespace Lime.Protocol.Listeners
{
    /// <summary>
    /// Creates listener loop tasks that receive envelopes from the channel and calls a consumer delegate.
    /// A task is created for each envelope type.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public sealed class ChannelListener : IChannelListener, IDisposable
    {        
        private readonly Func<Message, Task<bool>> _messageConsumer;
        private readonly Func<Notification, Task<bool>> _notificationConsumer;
        private readonly Func<Command, Task<bool>> _commandConsumer;
        private readonly CancellationTokenSource _cts;
        private readonly object _syncRoot;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelListener"/> class.
        /// </summary>
        /// <param name="messageConsumer">The message consumer.</param>
        /// <param name="notificationConsumer">The notification consumer.</param>
        /// <param name="commandConsumer">The command consumer.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public ChannelListener(Func<Message, Task<bool>> messageConsumer, Func<Notification, Task<bool>> notificationConsumer, Func<Command, Task<bool>> commandConsumer)
        {            
            if (messageConsumer == null) throw new ArgumentNullException(nameof(messageConsumer));
            if (notificationConsumer == null) throw new ArgumentNullException(nameof(notificationConsumer));
            if (commandConsumer == null) throw new ArgumentNullException(nameof(commandConsumer));

            _messageConsumer = messageConsumer;
            _notificationConsumer = notificationConsumer;
            _commandConsumer = commandConsumer;
            _cts = new CancellationTokenSource();
            _syncRoot = new object();
        }

        /// <summary>
        /// Starts the channel listener tasks.
        /// </summary>
        /// <param name="channel"></param>
        public void Start(IEstablishedReceiverChannel channel)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            lock (_syncRoot)
            {
                if (MessageListenerTask == null)
                {
                    MessageListenerTask = CreateListenerTask(
                            channel.ReceiveMessageAsync,
                            _messageConsumer);
                }

                if (NotificationListenerTask == null)
                {
                    NotificationListenerTask = CreateListenerTask(
                        channel.ReceiveNotificationAsync,
                        _notificationConsumer);
                }

                if (CommandListenerTask == null)
                {
                    CommandListenerTask = CreateListenerTask(
                        channel.ReceiveCommandAsync,
                        _commandConsumer);
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
        }

        private async Task<T> CreateListenerTask<T>(Func<CancellationToken, Task<T>> producer, Func<T, Task<bool>> consumer)
        {
            try
            {
                return await ProducerConsumer.CreateAsync(
                    producer,
                    consumer,
                    _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                return default(T);
            }
        }
    }
}