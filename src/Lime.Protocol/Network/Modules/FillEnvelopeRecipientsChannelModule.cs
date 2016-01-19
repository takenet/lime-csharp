using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Client;

namespace Lime.Protocol.Network.Modules
{
    /// <summary>
    /// Defines a channel module that fills envelope receipts based on the channel information.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="ChannelModuleBase{T}" />
    public sealed class FillEnvelopeRecipientsChannelModule<T> : ChannelModuleBase<T> where T : Envelope, new()
    {
        private readonly IChannel _channel;

        public FillEnvelopeRecipientsChannelModule(IChannel channel)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            _channel = channel;
        }

        public override Task<T> OnSendingAsync(T envelope, CancellationToken cancellationToken)
        {
            if (_channel is ClientChannel &&
                _channel.LocalNode != null)
            {
                if (envelope.Pp == null)
                {
                    if (envelope.From != null &&
                        !envelope.From.Equals(_channel.LocalNode))
                    {
                        envelope.Pp = _channel.LocalNode.Copy();
                    }
                }
                else if (string.IsNullOrWhiteSpace(envelope.Pp.Domain))
                {
                    envelope.Pp.Domain = _channel.LocalNode.Domain;
                }
            }

            return envelope.AsCompletedTask();
        }


        public override Task<T> OnReceivingAsync(T envelope, CancellationToken cancellationToken)
        {
            var from = _channel.RemoteNode;
            var to = _channel.LocalNode;

            if (from != null)
            {
                if (envelope.From == null)
                {
                    envelope.From = from.Copy();
                }
                else if (string.IsNullOrEmpty(envelope.From.Domain))
                {
                    envelope.From.Domain = from.Domain;
                }
            }

            if (to != null)
            {
                if (envelope.To == null)
                {
                    envelope.To = to.Copy();
                }
                else if (string.IsNullOrEmpty(envelope.To.Domain))
                {
                    envelope.To.Domain = to.Domain;
                }
            }

            return envelope.AsCompletedTask();
        }
    }

    /// <summary>
    /// Wrapper class for <see cref="FillEnvelopeRecipientsChannelModule{T}"/> instances.
    /// </summary>
    /// <seealso cref="ChannelModuleBase{T}" />
    public sealed class FillEnvelopeRecipientsChannelModule
    {
        private FillEnvelopeRecipientsChannelModule(FillEnvelopeRecipientsChannelModule<Message> messageChannelModule, FillEnvelopeRecipientsChannelModule<Notification> notificationChannelModule, FillEnvelopeRecipientsChannelModule<Command> commandChannelModule)
        {
            MessageChannelModule = messageChannelModule;
            NotificationChannelModule = notificationChannelModule;
            CommandChannelModule = commandChannelModule;
        }

        public FillEnvelopeRecipientsChannelModule<Message> MessageChannelModule { get; }

        public FillEnvelopeRecipientsChannelModule<Notification> NotificationChannelModule { get; }

        public FillEnvelopeRecipientsChannelModule<Command> CommandChannelModule { get; }


        public static implicit operator FillEnvelopeRecipientsChannelModule<Message>(
            FillEnvelopeRecipientsChannelModule fillEnvelopeRecipientsChannelModule)
        {
            return fillEnvelopeRecipientsChannelModule.MessageChannelModule;
        }

        public static implicit operator FillEnvelopeRecipientsChannelModule<Notification>(
            FillEnvelopeRecipientsChannelModule fillEnvelopeRecipientsChannelModule)
        {
            return fillEnvelopeRecipientsChannelModule.NotificationChannelModule;
        }

        public static implicit operator FillEnvelopeRecipientsChannelModule<Command>(
            FillEnvelopeRecipientsChannelModule fillEnvelopeRecipientsChannelModule)
        {
            return fillEnvelopeRecipientsChannelModule.CommandChannelModule;
        }

        /// <summary>
        /// Creates a new instance of<see cref= "FillEnvelopeRecipientsChannelModule" /> class and register it for all envelope types into the specified channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        public static FillEnvelopeRecipientsChannelModule CreateAndRegister(IChannel channel)
        {
            var fillEnvelopeRecipientsChannelModule = new FillEnvelopeRecipientsChannelModule(
                new FillEnvelopeRecipientsChannelModule<Message>(channel),
                new FillEnvelopeRecipientsChannelModule<Notification>(channel),
                new FillEnvelopeRecipientsChannelModule<Command>(channel));
            channel.MessageModules.Add(fillEnvelopeRecipientsChannelModule.MessageChannelModule);
            channel.NotificationModules.Add(fillEnvelopeRecipientsChannelModule.NotificationChannelModule);
            channel.CommandModules.Add(fillEnvelopeRecipientsChannelModule.CommandChannelModule);
            return fillEnvelopeRecipientsChannelModule;

        }
    }
}
