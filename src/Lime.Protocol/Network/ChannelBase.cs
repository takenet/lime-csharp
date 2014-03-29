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
        #region Constructors

        public ChannelBase(ITransport transport)
        {
            if (transport == null)
            {
                throw new ArgumentNullException("transport");
            }

            this.Transport = transport;
            this.Transport.EnvelopeReceived += Transport_EnvelopeReceived;

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
        public SessionState State { get; private set; }

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

            return this.Transport.SendAsync(message);
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
                throw new ArgumentNullException("message");
            }

            if (this.State != SessionState.Established)
            {
                throw new InvalidOperationException(string.Format("Cannot send a command in the '{0}' session state", this.State));
            }

            return this.Transport.SendAsync(command);
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

            return this.Transport.SendAsync(notification);
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

            return this.Transport.SendAsync(session);
        }

        /// <summary>
        /// Occurs when the session state
        /// is changed in the remote node
        /// </summary>
        public event EventHandler<EnvelopeEventArgs<Session>> SessionReceived;

        #endregion

        /// <summary>
        /// Handles the EnvelopeReceived event of the Transport.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EnvelopeEventArgs{Envelope}"/> instance containing the event data.</param>
        private async void Transport_EnvelopeReceived(object sender, EnvelopeEventArgs<Envelope> e)
        {
            if (e.Envelope is Notification)
            {
                await this.OnNotificationReceivedAsync((Notification)e.Envelope);
            }
            else if (e.Envelope is Message)
            {
                await this.OnMessageReceivedAsync((Message)e.Envelope);
            }
            else if (e.Envelope is Command)
            {
                await this.OnCommandReceivedAsync((Command)e.Envelope);
            }
            else if (e.Envelope is Session)
            {
                await this.OnSessionReceivedAsync((Session)e.Envelope);
            }
        }

        #region Protected Members

        protected virtual Task OnMessageReceivedAsync(Message message)
        {
            this.MessageReceived.RaiseEvent(this, new EnvelopeEventArgs<Message>(message));
            return Task.FromResult<object>(null);
        }

        protected virtual Task OnCommandReceivedAsync(Command command)
        {
            this.CommandReceived.RaiseEvent(this, new EnvelopeEventArgs<Command>(command));
            return Task.FromResult<object>(null);
        }

        protected virtual Task OnNotificationReceivedAsync(Notification notification)
        {
            this.NotificationReceived.RaiseEvent(this, new EnvelopeEventArgs<Notification>(notification));
            return Task.FromResult<object>(null);
        }

        protected virtual Task OnSessionReceivedAsync(Session session)
        {
            this.SessionReceived.RaiseEvent(this, new EnvelopeEventArgs<Session>(session));
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
