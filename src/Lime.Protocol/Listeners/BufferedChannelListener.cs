using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Util;

namespace Lime.Protocol.Listeners
{
    public class BufferedChannelListener : IChannelListener, IDisposable
    {
        private readonly Func<Message, CancellationToken, Task<bool>> _messageConsumer;
        private readonly Func<Notification, CancellationToken, Task<bool>> _notificationConsumer;
        private readonly Func<Command, CancellationToken, Task<bool>> _commandConsumer;
        private readonly object _syncRoot;
        private CancellationTokenSource _cts;
        public BufferedChannelListener(
            Func<Message, CancellationToken, Task<bool>> messageConsumer,
            Func<Notification, CancellationToken, Task<bool>> notificationConsumer,
            Func<Command, CancellationToken, Task<bool>> commandConsumer,
            int capacity = 1)
        {
            _messageConsumer = messageConsumer ?? throw new ArgumentNullException(nameof(messageConsumer));
            _notificationConsumer = notificationConsumer ?? throw new ArgumentNullException(nameof(notificationConsumer));
            _commandConsumer = commandConsumer ?? throw new ArgumentNullException(nameof(commandConsumer));
            MessageBuffer = ChannelUtil.CreateForCapacity<Message>(capacity, singleWriter: true, singleReader: true);
            NotificationBuffer = ChannelUtil.CreateForCapacity<Notification>(capacity, singleWriter: true, singleReader: true);
            CommandBuffer = ChannelUtil.CreateForCapacity<Command>(capacity, singleWriter: true, singleReader: true);
            _syncRoot = new object();
        }
        
        public Channel<Message> MessageBuffer { get; }
        
        public Channel<Notification> NotificationBuffer { get; }
        
        public Channel<Command> CommandBuffer { get; }
        
        public Task<Message> MessageListenerTask { get; private set; }
        
        public Task<Notification> NotificationListenerTask { get; private set; }
        
        public Task<Command> CommandListenerTask { get; private set; }
        
        public void Start(IEstablishedReceiverChannel channel)
        {
            lock (_syncRoot)
            {
                if (_cts != null && !_cts.IsCancellationRequested)
                {
                    throw new InvalidOperationException("The listener is already active");
                }
                _cts = new CancellationTokenSource();

                MessageListenerTask = CreateListenerTask(
                    channel.ReceiveMessageAsync,
                    MessageBuffer,
                    _messageConsumer,
                    _cts.Token);
                
                NotificationListenerTask = CreateListenerTask(
                    channel.ReceiveNotificationAsync,
                    NotificationBuffer,
                    _notificationConsumer,
                    _cts.Token);
                
                CommandListenerTask = CreateListenerTask(
                    channel.ReceiveCommandAsync,
                    CommandBuffer,
                    _commandConsumer,
                    _cts.Token);
            }
        }

        public void Stop()
        {
            lock (_syncRoot)
            {
                if (_cts == null || _cts.IsCancellationRequested)
                {
                    throw new InvalidOperationException("The listener is not active");
                }

                _cts.Cancel();                
                MessageBuffer.Writer.TryComplete();
                NotificationBuffer.Writer.TryComplete();
                CommandBuffer.Writer.TryComplete();
            }
        }
        
        private async Task<T> CreateListenerTask<T>(
            Func<CancellationToken, Task<T>> producer, 
            Channel<T> channel,
            Func<T, CancellationToken, Task<bool>> consumer,
            CancellationToken cancellationToken)
        {
            using var cts = new CancellationTokenSource();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            
            var producerToChannelTask = ProducerConsumer.CreateAsync(
                producer,
                async (i, c) =>
                {
                    await channel.Writer.WriteAsync(i, c);
                    return true;
                },
                linkedCts.Token,
                true);
            var channelToConsumerTask = ProducerConsumer.CreateAsync(
                (c) => channel.Reader.ReadAsync(c).AsTask(),
                consumer,
                linkedCts.Token,
                true);

            await Task.WhenAny(producerToChannelTask, channelToConsumerTask);
            
            // Cancel and awaits the both tasks again
            cts.Cancel();
            
            var items = await Task.WhenAll(producerToChannelTask, channelToConsumerTask);
            
            return items.FirstOrDefault(i => i != null);
        }
        
        /// <summary>
        /// Stops the listener tasks and releases any related resource.
        /// </summary>
        public void Dispose()
        {
            _cts?.CancelAndDispose();
        }
    }
}