using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol.Security;
using Lime.Protocol.Network;
using Lime.Protocol.Resources;
using System.Threading;

namespace Lime.Protocol.Client
{
    /// <summary>
    /// Defines the communication channel
    /// between a node and a server
    /// </summary>
    public class ClientChannel : ChannelBase, IClientChannel, IDisposable
    {
        #region Private fields

        private bool _autoReplyPings;
        private bool _autoNotifyReceipt;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientChannel"/> class.
        /// </summary>
        /// <param name="transport">The transport to be used by the channel.</param>
        /// <param name="sendTimeout">The channel send timeout.</param>
        /// <param name="autoReplyPings">Indicates if the client should reply automatically to server ping commands.</param>
        /// <param name="autoNotifyReceipt">Indicates if the client should automatically send 'received' notifications for messages.</param>
        public ClientChannel(ITransport transport, TimeSpan sendTimeout, bool autoReplyPings = true, bool autoNotifyReceipt = false)
            : base(transport, sendTimeout)
        {
            _autoReplyPings = autoReplyPings;
            _autoNotifyReceipt = autoNotifyReceipt;
        }

        ~ClientChannel()
        {
            Dispose(false);
        }

        #endregion

        #region IClientChannel Members

        /// <summary>
        /// Sends a new session envelope
        /// to the server to start a
        /// session negotiation
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Invalid session state</exception>
        public Task SendNewSessionAsync()
        {
            if (base.State != SessionState.New)
            {
                throw new InvalidOperationException(string.Format("Cannot start a session in the '{0}' state", base.State));
            }

            var session = new Session()
            {
                State = SessionState.New
            };

            return base.SendSessionAsync(session);
        }

        /// <summary>
        /// Send a authenticate session envelope
        /// to the server to establish
        /// an authenticated session
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="authentication">Authentication information.</param>
        /// <param name="instance"></param>
        /// <param name="sessionMode"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        /// <exception cref="System.ArgumentNullException">
        /// identity
        /// or
        /// authentication
        /// </exception>
        public Task SendAuthenticatingSessionAsync(Identity identity, Authentication authentication, string instance = null, SessionMode sessionMode = SessionMode.Node)
        {
            if (base.State != SessionState.Authenticating)
            {
                throw new InvalidOperationException(string.Format("Cannot authenticate a session in the '{0}' state", base.State));
            }

            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }

            if (authentication == null)
            {
                throw new ArgumentNullException("authentication");
            }

            var session = new Session()
            {                
                Id = base.SessionId,
                From = new Node()
                {
                    Name = identity.Name,
                    Domain = identity.Domain,
                    Instance = instance
                },
                State = SessionState.Authenticating,
                Mode = sessionMode,
                Authentication = authentication
            };

            return base.SendSessionAsync(session);
        }

        /// <summary>
        /// Sends a finish session envelope
        /// to the server to finish the session
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task SendFinishingSessionAsync()
        {
            if (base.State != SessionState.Established)
            {
                throw new InvalidOperationException(string.Format("Cannot finish a session in the '{0}' state", base.State));
            }

            var session = new Session()
            {
                Id = base.SessionId,
                State = SessionState.Finishing
            };

            return base.SendSessionAsync(session);
        }

        /// <summary>
        /// Occurs when the session is established
        /// with the server
        /// </summary>
        public event EventHandler<EnvelopeEventArgs<Session>> SessionEstablished;

        /// <summary>
        /// Occurs when the session fails
        /// with the server
        /// </summary>
        public event EventHandler<EnvelopeEventArgs<Session>> SessionFailed;

        /// <summary>
        /// Occurs when the session ends
        /// with the server
        /// </summary>
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

        /// <summary>
        /// Raises the MessageReceived event
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected async override Task OnMessageReceivedAsync(Message message)
        {
            await base.OnMessageReceivedAsync(message).ConfigureAwait(false);

            if (_autoNotifyReceipt &&
                message.Id.HasValue &&
                message.From != null)
            {
                await SendReceivedNotificationAsync(message.Id.Value, message.From).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Raises the CommandReceived event
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        protected async override Task OnCommandReceivedAsync(Command command)
        {
            await base.OnCommandReceivedAsync(command).ConfigureAwait(false);

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

                await SendCommandAsync(pingCommandResponse).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Raises the SessionReceived event and
        /// updates the channel session properties
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected async override Task OnSessionReceivedAsync(Session session)
        {           
            this.SessionId = session.Id.Value;
            this.State = session.State;

            if (session.State == SessionState.Established &&
                session.Id.HasValue)
            {
                this.LocalNode = session.To;
                this.RemoteNode = session.From;
            }

            await base.OnSessionReceivedAsync(session).ConfigureAwait(false);

            switch (session.State)
            {
                case SessionState.Established:
                    this.SessionEstablished.RaiseEvent(this, new EnvelopeEventArgs<Session>(session));
                    break;
                case SessionState.Finished:
                    this.SessionFinished.RaiseEvent(this, new EnvelopeEventArgs<Session>(session));                    
                    await this.Transport.CloseAsync(CancellationToken.None);
                    break;
                case SessionState.Failed:
                    this.SessionFailed.RaiseEvent(this, new EnvelopeEventArgs<Session>(session));
                    break;
            }
        }

        #endregion
    }
}
