using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Network;

namespace Lime.Protocol.Adapters
{
    public sealed class DataflowChannelAdapter : IDisposable
    {        
        private readonly CancellationTokenSource _cts;

        public DataflowChannelAdapter(IChannel channel)
            : this(channel, channel, channel)
        {
            
        }

        public DataflowChannelAdapter(IChannel channel, DataflowBlockOptions dataflowBlockOptions)
            : this(channel, channel, channel, dataflowBlockOptions)
        {

        }

        public DataflowChannelAdapter(IMessageChannel messageChannel, INotificationChannel notificationChannel, ICommandChannel commandChannel)
            : this(messageChannel, notificationChannel, commandChannel, new DataflowBlockOptions())
        {
            
        }

        public DataflowChannelAdapter(IMessageChannel messageChannel, INotificationChannel notificationChannel, ICommandChannel commandChannel, DataflowBlockOptions dataflowBlockOptions)
        {
            if (messageChannel == null) throw new ArgumentNullException(nameof(messageChannel));
            if (notificationChannel == null) throw new ArgumentNullException(nameof(notificationChannel));
            if (commandChannel == null) throw new ArgumentNullException(nameof(commandChannel));
            MessageBufferBlock = new BufferBlock<Message>(dataflowBlockOptions);
            NotificationBufferBlock = new BufferBlock<Notification>(dataflowBlockOptions);
            CommandBufferBlock = new BufferBlock<Command>(dataflowBlockOptions);
            _cts = new CancellationTokenSource();

            MessageListenerTask = Listener.ListenAsync(messageChannel.ReceiveMessageAsync,
                MessageBufferBlock.SendAsync, _cts.Token);
            NotificationListenerTask = Listener.ListenAsync(notificationChannel.ReceiveNotificationAsync,
                NotificationBufferBlock.SendAsync, _cts.Token);
            CommandListenerTask = Listener.ListenAsync(commandChannel.ReceiveCommandAsync,
                CommandBufferBlock.SendAsync, _cts.Token);
        }

        public Task MessageListenerTask { get; }
        public Task NotificationListenerTask { get; }
        public Task CommandListenerTask { get; }

        public BufferBlock<Message> MessageBufferBlock { get; }

        public BufferBlock<Notification> NotificationBufferBlock { get; }

        public BufferBlock<Command> CommandBufferBlock { get; }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            MessageListenerTask.Dispose();
            NotificationListenerTask.Dispose();
            CommandListenerTask.Dispose();            
        }
    }
}
