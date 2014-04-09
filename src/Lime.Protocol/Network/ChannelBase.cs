using Lime.Protocol.Util;
using System;
using System.Collections.Concurrent;
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
    public abstract class ChannelBase : IChannel, ISessionChannel, IDisposable
    {
        #region Fields
        
        private TimeSpan _sendTimeout;
        private SemaphoreSlim _receiveSemaphore = new SemaphoreSlim(1);
        protected IAsyncQueue<Message> _messageAsyncBuffer;
        protected IAsyncQueue<Command> _commandAsyncBuffer;
        protected IAsyncQueue<Notification> _notificationAsyncBuffer;
        protected IAsyncQueue<Session> _sessionAsyncBuffer;        
        protected CancellationTokenSource _channelCancellationTokenSource;

        #endregion

        #region Constructors

        public ChannelBase(ITransport transport, TimeSpan sendTimeout)
        {
            if (transport == null)
            {
                throw new ArgumentNullException("transport");
            }

            this.Transport = transport;
            this.Transport.Closing += Transport_Closing;

            _sendTimeout = sendTimeout;            

            _channelCancellationTokenSource = new CancellationTokenSource();

            this.State = SessionState.New;

            _messageAsyncBuffer = new AsyncQueue<Message>();
            _messageAsyncBuffer.PromiseAdded += EnvelopeAsyncBuffer_PromiseAdded;
            _commandAsyncBuffer = new AsyncQueue<Command>();
            _commandAsyncBuffer.PromiseAdded += EnvelopeAsyncBuffer_PromiseAdded;
            _notificationAsyncBuffer = new AsyncQueue<Notification>();
            _notificationAsyncBuffer.PromiseAdded += EnvelopeAsyncBuffer_PromiseAdded;
            _sessionAsyncBuffer = new AsyncQueue<Session>(1, 0);
            _sessionAsyncBuffer.PromiseAdded += EnvelopeAsyncBuffer_PromiseAdded;            
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
        /// Receives a message
        /// from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            if (this.State != SessionState.Established)
            {
                throw new InvalidOperationException(string.Format("Cannot receive a message in the '{0}' session state", this.State));
            }

            var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                _channelCancellationTokenSource.Token,
                cancellationToken);

            return _messageAsyncBuffer.DequeueAsync(combinedCancellationTokenSource.Token);
        }      

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
        /// Receives a command
        /// from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<Command> ReceiveCommandAsync(CancellationToken cancellationToken)
        {
            if (this.State != SessionState.Established)
            {
                throw new InvalidOperationException(string.Format("Cannot receive a command in the '{0}' session state", this.State));
            }

            var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                _channelCancellationTokenSource.Token,
                cancellationToken);

            return _commandAsyncBuffer.DequeueAsync(combinedCancellationTokenSource.Token);
        }

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
        /// Receives a notification
        /// from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<Notification> ReceiveNotificationAsync(CancellationToken cancellationToken)
        {
            if (this.State != SessionState.Established)
            {
                throw new InvalidOperationException(string.Format("Cannot receive a notification in the '{0}' session state", this.State));
            }

            var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                _channelCancellationTokenSource.Token,
                cancellationToken);

            return _notificationAsyncBuffer.DequeueAsync(combinedCancellationTokenSource.Token);
        }

        #endregion

        #region ISessionChannel Members

        /// <summary>
        /// Sends a session change message to 
        /// the remote node. 
        /// Avoid to use this method directly. Instead,
        /// use the Server or Client channel methods.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">session</exception>
        Task ISessionChannel.SendSessionAsync(Session session)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }

            return this.SendAsync(session);
        }

        /// <summary>
        /// Receives a session
        /// from the remote node.
        /// Avoid to use this method directly. Instead,
        /// use the Server or Client channel methods.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        Task<Session> ISessionChannel.ReceiveSessionAsync(CancellationToken cancellationToken)
        {
            if (this.State == SessionState.Finished)
            {
                throw new InvalidOperationException(string.Format("Cannot receive a session in the '{0}' session state", this.State));
            }

            var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                _channelCancellationTokenSource.Token,
                cancellationToken);

            return _sessionAsyncBuffer.DequeueAsync(combinedCancellationTokenSource.Token);     
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sends the envelope to the transport
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        private Task SendAsync(Envelope envelope)
        {
            var timeoutCancellationTokenSource = new CancellationTokenSource(_sendTimeout);
            return this.Transport.SendAsync(
                envelope,
                CancellationTokenSource.CreateLinkedTokenSource(
                    _channelCancellationTokenSource.Token,
                    timeoutCancellationTokenSource.Token).Token);
        }

        /// <summary>
        /// Consumes envelopes from the transport
        /// while there are any pending promise in
        /// the buffers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void EnvelopeAsyncBuffer_PromiseAdded(object sender, EventArgs e)
        {
            await _receiveSemaphore.WaitAsync(_channelCancellationTokenSource.Token).ConfigureAwait(false);

            try
            {
                while (_messageAsyncBuffer.HasPromises ||
                       _commandAsyncBuffer.HasPromises ||
                       _notificationAsyncBuffer.HasPromises ||
                       _sessionAsyncBuffer.HasPromises)
                {
                    var envelope = await this.Transport.ReceiveAsync(_channelCancellationTokenSource.Token).ConfigureAwait(false);

                    if (envelope is Notification)
                    {
                        await this.OnNotificationReceivedAsync((Notification)envelope).ConfigureAwait(false);
                    }
                    else if (envelope is Message)
                    {
                        await this.OnMessageReceivedAsync((Message)envelope).ConfigureAwait(false);
                    }
                    else if (envelope is Command)
                    {
                        await this.OnCommandReceivedAsync((Command)envelope).ConfigureAwait(false);
                    }
                    else if (envelope is Session)
                    {
                        await this.OnSessionReceivedAsync((Session)envelope).ConfigureAwait(false);
                    }
                    else
                    {
                        throw new InvalidOperationException("Invalid or unknown envelope received by the transport.");
                    }
                }
            }
            catch (InvalidOperationException)
            {
                this.Transport.CloseAsync(_channelCancellationTokenSource.Token).Wait();
                throw;
            }
            finally
            {
                _receiveSemaphore.Release();
            }
        }

        /// <summary>
        /// Cancels the token that is associated to 
        /// the channel send and receive tasks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Transport_Closing(object sender, DeferralEventArgs e)
        {
            using (e.GetDeferral())
            {
                _channelCancellationTokenSource.Cancel();
            }            
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Wraps the ISessionChannel.SendSessionAsync 
        /// method for the derivated classes.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected Task SendSessionAsync(Session session)
        {
            return ((ISessionChannel)this).SendSessionAsync(session);
        }

        /// <summary>
        /// Wraps the ISessionChannel.ReceiveSessionAsync 
        /// method for the derivated classes.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected Task<Session> ReceiveSessionAsync(CancellationToken cancellationToken)
        {
            return ((ISessionChannel)this).ReceiveSessionAsync(cancellationToken);
        }

        /// <summary>
        /// Raises the MessageReceived event
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected virtual Task OnMessageReceivedAsync(Message message)
        {
            if (this.State == SessionState.Established)
            {
                _messageAsyncBuffer.Enqueue(message);
                return Task.FromResult<object>(null);
            }
            else
            {
                throw new InvalidOperationException("A message was received in a invalid channel state");
            }
        }

        /// <summary>
        /// Raises the CommandReceived event
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected virtual Task OnCommandReceivedAsync(Command command)
        {
            if (this.State == SessionState.Established)
            {
                _commandAsyncBuffer.Enqueue(command);
                return Task.FromResult<object>(null);
            }
            else
            {
                throw new InvalidOperationException("A command was received in a invalid channel state");
            }
        }

        /// <summary>
        /// Raises the NotificationReceived event
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected virtual Task OnNotificationReceivedAsync(Notification notification)
        {
            if (this.State == SessionState.Established)
            {
                _notificationAsyncBuffer.Enqueue(notification);
                return Task.FromResult<object>(null);
            }
            else
            {
                throw new InvalidOperationException("A notification was received in a invalid channel state");
            }
        }

        /// <summary>
        /// Raises the SessionReceived event 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected virtual Task OnSessionReceivedAsync(Session session)
        {            
            _sessionAsyncBuffer.Enqueue(session);
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
                if (!_channelCancellationTokenSource.IsCancellationRequested)
                {
                    _channelCancellationTokenSource.Cancel();
                }

                _channelCancellationTokenSource.Dispose();
                this.Transport.DisposeIfDisposable();
            }
        }

        #endregion

    }
}
