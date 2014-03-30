using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Base class for the protocol
    /// communication channels
    /// </summary>
    public abstract class ChannelBase : IChannel, IDisposable
    {
        private TimeSpan _sendTimeout;

        #region Constructors

        public ChannelBase(ITransport transport, TimeSpan sendTimeout)
        {
            if (transport == null)
            {
                throw new ArgumentNullException("transport");
            }

            this.Transport = transport;
            this.Transport.EnvelopeReceived += Transport_EnvelopeReceived;

            _sendTimeout = sendTimeout;

            this.State = SessionState.New;
        }

        ~ChannelBase()
        {
            Dispose(false);
        }

        #endregion

        #region IChannel Members

        /// <summary>
        /// The current session transport
        /// </summary>
        public ITransport Transport { get; private set; }

        /// <summary>
        /// Remote node identifier
        /// </summary>
        public Node RemoteNode { get; protected set; }

        /// <summary>
        /// Remote node identifier
        /// </summary>
        public Node LocalNode { get; protected set; }

        /// <summary>
        /// The session Id
        /// </summary>
        public Guid SessionId { get; protected set; }

        /// <summary>
        /// Current session state
        /// </summary>
        public SessionState State { get; protected set; }

        /// <summary>
        /// Current session mode
        /// </summary>
        public SessionMode Mode { get; protected set; }

        /// <summary>
        /// Occours when a session negotiation option
        /// is received by the node or server
        /// </summary>
        public event EventHandler<EnvelopeEventArgs<Session>> NegotiateSessionReceived;

        /// <summary>
        /// Occours when a session authentication option
        /// is received by the node or server
        /// </summary>
        public event EventHandler<EnvelopeEventArgs<Session>> AuthenticateSessionReceived;

        /// <summary>
        /// Send a negotiate session envelope
        /// to the remote node to choose or
        /// confirm the session negotiation options
        /// parameters
        /// </summary>
        /// <param name="sessionCompression">The session compression option</param>
        /// <param name="sessionEncryption">The session encryption option</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task SendNegotiatingSessionAsync(SessionCompression sessionCompression, SessionEncryption sessionEncryption)
        {
            if (this.State != SessionState.Negotiating)
            {
                throw new InvalidOperationException(string.Format("Cannot negotiate a session in the '{0}' state", this.State));
            }

            var session = new Session()
            {
                Id = this.SessionId,
                State = SessionState.Negotiating,
                Compression = sessionCompression,
                Encryption = sessionEncryption
            };

            return this.SendSessionAsync(session);
        }

        #endregion

        #region IMessageChannel Members

        /// <summary>
        /// Sends a message to the
        /// remote node
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">message</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task SendMessageAsync(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (this.State != SessionState.Established)
            {
                throw new InvalidOperationException(string.Format("Cannot send a message in the '{0}' session state", this.State));
            }

            
            return this.SendAsync(message);
        }

        /// <summary>
        /// Occurs when a message is 
        /// received by the node
        /// </summary>
        public event EventHandler<EnvelopeEventArgs<Message>> MessageReceived;

        #endregion

        #region ICommandChannel Members

        /// <summary>
        /// Sends a command envelope to
        /// the remote node
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">message</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task SendCommandAsync(Command command)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            if (this.State != SessionState.Established)
            {
                throw new InvalidOperationException(string.Format("Cannot send a command in the '{0}' session state", this.State));
            }

            return this.SendAsync(command);
        }

        /// <summary>
        /// Occurs when a command envelope 
        /// is received by the node
        /// </summary>
        public event EventHandler<EnvelopeEventArgs<Command>> CommandReceived;

        #endregion

        #region INotificationChannel Members

        /// <summary>
        /// Sends a notification to the
        /// remote node
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">notification</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task SendNotificationAsync(Notification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException("notification");
            }

            if (this.State != SessionState.Established)
            {
                throw new InvalidOperationException(string.Format("Cannot send a notification in the '{0}' session state", this.State));
            }

            return this.SendAsync(notification);
        }

        /// <summary>
        /// Occurs when a notification is 
        /// received by the node
        /// </summary>
        public event EventHandler<EnvelopeEventArgs<Notification>> NotificationReceived;

        #endregion

        #region ISessionChannel Members

        /// <summary>
        /// Sends a session change message to 
        /// the remote node
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">session</exception>
        public Task SendSessionAsync(Session session)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }

            return this.SendAsync(session);
        }

        /// <summary>
        /// Occurs when the session state
        /// is changed in the remote node
        /// </summary>
        public event EventHandler<EnvelopeEventArgs<Session>> SessionReceived;

        #endregion

        /// <summary>
        /// Sends the envelope to the transport
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        private Task SendAsync(Envelope envelope)
        {
            var cts = new CancellationTokenSource(_sendTimeout);
            return this.Transport.SendAsync(envelope, cts.Token);
        }

        /// <summary>
        /// Handles the EnvelopeReceived event of the Transport.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EnvelopeEventArgs{Envelope}"/> instance containing the event data.</param>
        private async void Transport_EnvelopeReceived(object sender, EnvelopeEventArgs<Envelope> e)
        {
            if (e.Envelope is Notification)
            {
                await this.OnNotificationReceivedAsync((Notification)e.Envelope).ConfigureAwait(false);
            }
            else if (e.Envelope is Message)
            {
                await this.OnMessageReceivedAsync((Message)e.Envelope).ConfigureAwait(false);
            }
            else if (e.Envelope is Command)
            {
                await this.OnCommandReceivedAsync((Command)e.Envelope).ConfigureAwait(false);
            }
            else if (e.Envelope is Session)
            {
                await this.OnSessionReceivedAsync((Session)e.Envelope).ConfigureAwait(false);
            }
        }

        #region Protected Members

        /// <summary>
        /// Raises the MessageReceived event
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected virtual Task OnMessageReceivedAsync(Message message)
        {
            this.MessageReceived.RaiseEvent(this, new EnvelopeEventArgs<Message>(message));
            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Raises the CommandReceived event
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected virtual Task OnCommandReceivedAsync(Command command)
        {
            this.CommandReceived.RaiseEvent(this, new EnvelopeEventArgs<Command>(command));
            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Raises the NotificationReceived event
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected virtual Task OnNotificationReceivedAsync(Notification notification)
        {
            this.NotificationReceived.RaiseEvent(this, new EnvelopeEventArgs<Notification>(notification));
            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Raises the SessionReceived event 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected virtual Task OnSessionReceivedAsync(Session session)
        {            
            this.SessionReceived.RaiseEvent(this, new EnvelopeEventArgs<Session>(session));

            switch (session.State)
            {
                case SessionState.Negotiating:
                    this.NegotiateSessionReceived.RaiseEvent(this, new EnvelopeEventArgs<Session>(session));
                    break;
                case SessionState.Authenticating:
                    this.AuthenticateSessionReceived.RaiseEvent(this, new EnvelopeEventArgs<Session>(session));
                    break;
            }

            return Task.FromResult<object>(null);
        }

        #endregion


        #region IDisposable Members

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
                this.Transport.DisposeIfDisposable();
            }
        }

        #endregion
    }
}
