using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol.Network;

namespace Lime.Protocol.Adapters
{
    public sealed class EventChannelListenerAdapter : ChannelListenerAdapterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventChannelListenerAdapter"/> class.
        /// </summary>
        /// <param name="channel">The channel.</param>
        public EventChannelListenerAdapter(IChannel channel)
            : base(channel, channel, channel)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventChannelListenerAdapter"/> class.
        /// </summary>
        /// <param name="messageChannel">The message channel.</param>
        /// <param name="notificationChannel">The notification channel.</param>
        /// <param name="commandChannel">The command channel.</param>
        public EventChannelListenerAdapter(IMessageChannel messageChannel, INotificationChannel notificationChannel, ICommandChannel commandChannel) 
            : base(messageChannel, notificationChannel, commandChannel)
        {
            StartListenerTasks(RaiseMessageReceivedAsync, RaiseNotificationReceivedAsync, RaiseCommandReceivedAsync);
        }

        private async Task RaiseMessageReceivedAsync(Message envelope)
        {
            var eventArgs = new EnvelopeEventArgs<Message>(envelope);
            MessageReceived?.RaiseEvent(this,eventArgs);
            await eventArgs.WaitForDeferralsAsync().ConfigureAwait(false);
        }

        private async Task RaiseNotificationReceivedAsync(Notification envelope)
        {
            var eventArgs = new EnvelopeEventArgs<Notification>(envelope);
            NotificationReceived?.RaiseEvent(this, eventArgs);
            await eventArgs.WaitForDeferralsAsync().ConfigureAwait(false);
        }

        private async Task RaiseCommandReceivedAsync(Command envelope)
        {
            var eventArgs = new EnvelopeEventArgs<Command>(envelope);
            CommandReceived?.RaiseEvent(this, eventArgs);
            await eventArgs.WaitForDeferralsAsync().ConfigureAwait(false);
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
    }
}
