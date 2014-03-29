using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol.Security;
using Lime.Protocol.Network;
using Lime.Protocol.Resources;

namespace Lime.Protocol.Client
{
    public class LimeClient : ChannelBase, IClientChannel, IDisposable
    {
        #region Private fields

        private bool _autoReplyPings;
        private bool _autoNotifyReceipt;

        #endregion

        #region Constructor

        public LimeClient(ITransport transport, bool autoReplyPings = true, bool autoNotifyReceipt = false)
            : base(transport)
        {
            _autoReplyPings = autoReplyPings;
            _autoNotifyReceipt = autoNotifyReceipt;
        }

        ~LimeClient()
        {
            Dispose(false);
        }

        #endregion

        #region IClientChannel Members

        public Task SendNewSessionAsync()
        {
            throw new NotImplementedException();
        }

        public Task SendNegotiateSessionAsync(SessionCompression sessionCompression = SessionCompression.None, SessionEncryption sessionEncryption = SessionEncryption.TLS)
        {
            throw new NotImplementedException();
        }

        public Task SendAuthenticateSessionAsync(Identity identity, Authentication authentication, string instance = null, SessionMode sessionMode = SessionMode.Node)
        {
            throw new NotImplementedException();
        }

        public Task SendFinishSessionAsync()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<EnvelopeEventArgs<Session>> SessionEstablished;

        public event EventHandler<EnvelopeEventArgs<Session>> SessionFailed;

        public event EventHandler<EnvelopeEventArgs<Session>> SessionFinished;

        /// <summary>
        /// Notify to the server that
        /// the specified message was received
        /// by the peer
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">to</exception>
        public Task SendReceivedNotificationAsync(Guid messageId, Node to)
        {
            if (to == null)
            {
                throw new ArgumentNullException("to");
            }

            var notification = new Notification()
            {
                Id = messageId,
                To = to,
                Event = Event.Received
            };

            return this.SendNotificationAsync(notification);
        }

        #endregion

        #region ChannelBase Members

        protected async override Task OnMessageReceivedAsync(Message message)
        {
            await base.OnMessageReceivedAsync(message);

            if (_autoNotifyReceipt &&
                message.Id.HasValue &&
                message.From != null)
            {
                await SendReceivedNotificationAsync(message.Id.Value, message.From);
            }
        }

        protected async override Task OnCommandReceivedAsync(Command command)
        {
            await base.OnCommandReceivedAsync(command);

            if (_autoReplyPings &&
                command.Resource is Ping &&
                command.Status == CommandStatus.Pending &&
                command.Method == CommandMethod.Get)
            {
                var pingCommandResponse = new Command()
                {
                    Id = command.Id,
                    Status = CommandStatus.Success,
                    Method = CommandMethod.Get,
                    Resource = new Ping()
                };

                await SendCommandAsync(pingCommandResponse);
            }
        }

        #endregion


    }

}
