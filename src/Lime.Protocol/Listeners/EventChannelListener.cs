using System;
using System.Threading.Tasks;
using Lime.Protocol.Network;

namespace Lime.Protocol.Listeners
{
    public sealed class EventChannelListener : IChanneListener, IDisposable
    {
        private readonly ChannelListener _channelListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventChannelListener"/> class.
        /// </summary>
        /// <param name="channel">The channel.</param>
        public EventChannelListener(IEstablishedChannel channel)
            : this(channel, channel, channel)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventChannelListener"/> class.
        /// </summary>
        /// <param name="messageChannel">The message channel.</param>
        /// <param name="notificationChannel">The notification channel.</param>
        /// <param name="commandChannel">The command channel.</param>
        public EventChannelListener(IMessageChannel messageChannel, INotificationChannel notificationChannel, ICommandChannel commandChannel)             
        {
            _channelListener = new ChannelListener(
                messageChannel, notificationChannel, commandChannel,
                RaiseMessageReceivedAsync, RaiseNotificationReceivedAsync, RaiseCommandReceivedAsync);
        }

        private async Task<bool> RaiseMessageReceivedAsync(Message envelope)
        {
            var eventArgs = new EnvelopeEventArgs<Message>(envelope);
            MessageReceived?.RaiseEvent(this,eventArgs);
            await eventArgs.WaitForDeferralsAsync().ConfigureAwait(false);
            return true;
        }

        private async Task<bool> RaiseNotificationReceivedAsync(Notification envelope)
        {
            var eventArgs = new EnvelopeEventArgs<Notification>(envelope);
            NotificationReceived?.RaiseEvent(this, eventArgs);
            await eventArgs.WaitForDeferralsAsync().ConfigureAwait(false);
            return true;
        }

        private async Task<bool> RaiseCommandReceivedAsync(Command envelope)
        {
            var eventArgs = new EnvelopeEventArgs<Command>(envelope);
            CommandReceived?.RaiseEvent(this, eventArgs);
            await eventArgs.WaitForDeferralsAsync().ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Occurs when a <see cref="Message"/> was received by the channel.
        /// </summary>
        public event EventHandler<EnvelopeEventArgs<Message>> MessageReceived;

        /// <summary>
        /// Occurs when a <see cref="Notification"/> was received by the channel.
        /// </summary>
        public event EventHandler<EnvelopeEventArgs<Notification>> NotificationReceived;

        /// <summary>
        /// Occurs when a <see cref="Command"/> was received by the channel.
        /// </summary>
        public event EventHandler<EnvelopeEventArgs<Command>> CommandReceived;

        public void Start()
        {
            _channelListener.Start();
        }

        public void Stop()
        {
            _channelListener.Stop();
        }

        public Task MessageListenerTask => _channelListener.MessageListenerTask;

        public Task NotificationListenerTask => _channelListener.NotificationListenerTask;

        public Task CommandListenerTask => _channelListener.CommandListenerTask;

        public void Dispose()
        {
            _channelListener.Dispose();
        }
    }
}
