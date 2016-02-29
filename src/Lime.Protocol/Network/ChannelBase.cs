using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Util;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Network.Modules;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Base class for the protocol communication channels.
    /// </summary>
    public abstract class ChannelBase : IChannel, IDisposable
    {
        private readonly TimeSpan _sendTimeout;
        private readonly TimeSpan _closeTimeout;

        private readonly BufferBlock<Message> _messageBuffer;
        private readonly BufferBlock<Command> _commandBuffer;
        private readonly BufferBlock<Notification> _notificationBuffer;
        private readonly BufferBlock<Session> _sessionBuffer;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Command>> _pendingCommandsDictionary;

        private readonly CancellationTokenSource _consumerCts;
        private readonly object _syncRoot;
        private SessionState _state;
        private Task _consumeTransportTask;
        private bool _isConsumeTransportTaskFaulting;
        private bool _isDisposing;

        /// <summary>
        /// Creates a new instance of ChannelBase
        /// </summary>
        /// <param name="transport"></param>
        /// <param name="sendTimeout"></param>
        /// <param name="buffersLimit"></param>
        /// <param name="fillEnvelopeRecipients">Indicates if the from and to properties of sent and received envelopes should be filled with the session information if not defined.</param>
        /// <param name="autoReplyPings">Indicates if the channel should reply automatically to ping request commands. In this case, the ping command are not returned by the ReceiveCommandAsync method.</param>
        /// <param name="remotePingInterval">The interval to ping the remote party.</param>
        /// <param name="remoteIdleTimeout">The timeout to close the channel due to inactivity.</param>
        protected ChannelBase(ITransport transport, TimeSpan sendTimeout, int buffersLimit, bool fillEnvelopeRecipients, bool autoReplyPings, TimeSpan? remotePingInterval, TimeSpan? remoteIdleTimeout)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            Transport = transport;
            Transport.Closing += Transport_Closing;

            _sendTimeout = _closeTimeout = sendTimeout;     
            _consumerCts = new CancellationTokenSource();
            _syncRoot = new object();

            var options = new DataflowBlockOptions() { BoundedCapacity = buffersLimit };
            _messageBuffer = new BufferBlock<Message>(options);
            _commandBuffer = new BufferBlock<Command>(options);
            _notificationBuffer = new BufferBlock<Notification>(options);
            _sessionBuffer = new BufferBlock<Session>(new DataflowBlockOptions() { BoundedCapacity = 1 });
            _pendingCommandsDictionary = new ConcurrentDictionary<Guid, TaskCompletionSource<Command>>();
            MessageModules = new List<IChannelModule<Message>>();
            NotificationModules = new List<IChannelModule<Notification>>();
            CommandModules = new List<IChannelModule<Command>>();

            if (autoReplyPings)
            {
                CommandModules.Add(new ReplyPingChannelModule(this));
            }

            if (fillEnvelopeRecipients)
            {
                FillEnvelopeRecipientsChannelModule.CreateAndRegister(this);
            }

            if (remotePingInterval != null)
            {
                RemotePingChannelModule.CreateAndRegister(this, remotePingInterval.Value, remoteIdleTimeout);               
            }
        }

        ~ChannelBase()
        {
            Dispose(false);
        }

        #region IChannel Members

        /// <summary>
        /// The current session transport
        /// </summary>
        public ITransport Transport { get; }

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
        public SessionState State
        {
            get { return _state; }
            protected set
            {
                _state = value;

                if (_state == SessionState.Established)
                {
                    lock (_syncRoot)
                    {
                        if (_consumeTransportTask == null)
                        {
                            _consumeTransportTask = Task.Run(ConsumeTransportAsync);
                        }
                    }
                }
                
                OnStateChanged(MessageModules, _state);
                OnStateChanged(NotificationModules, _state);
                OnStateChanged(CommandModules, _state);
            }
        }

        /// <summary>
        /// Gets the message modules for processing sent and received messages.
        /// </summary>
        public ICollection<IChannelModule<Message>> MessageModules { get; }

        /// <summary>
        /// Gets the notification modules for processing sent and received notifications.
        /// </summary>
        public ICollection<IChannelModule<Notification>> NotificationModules { get; }

        /// <summary>
        /// Gets the command modules for processing sent and received commands.
        /// </summary>
        public ICollection<IChannelModule<Command>> CommandModules { get; }

        #endregion

        #region IMessageChannel Members

        /// <summary>
        /// Sends a message to the remote node.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">message</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public virtual Task SendMessageAsync(Message message) => SendAsync(message, MessageModules, CancellationToken.None);
        

        /// <summary>
        /// Receives a message from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public virtual Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
            => ReceiveEnvelopeAsync(_messageBuffer, cancellationToken);

        #endregion

        #region ICommandChannel Members

        /// <summary>
        /// Sends a command envelope to the remote node.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">message</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public virtual Task SendCommandAsync(Command command) => SendAsync(command, CommandModules, CancellationToken.None);

        /// <summary>
        /// Receives a command from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual Task<Command> ReceiveCommandAsync(CancellationToken cancellationToken)
            => ReceiveEnvelopeAsync(_commandBuffer, cancellationToken);

        /// <summary>
        /// Processes the command request.
        /// </summary>
        /// <param name="requestCommand">The request command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual async Task<Command> ProcessCommandAsync(Command requestCommand, CancellationToken cancellationToken)
        {
            if (requestCommand == null) throw new ArgumentNullException(nameof(requestCommand));
            if (requestCommand.Status != CommandStatus.Pending)
            {
                throw new ArgumentException("Invalid command status", nameof(requestCommand));
            }

            if (requestCommand.Method == CommandMethod.Observe)
            {
                throw new ArgumentException("Invalid command method", nameof(requestCommand));
            }

            if (requestCommand.Id == Guid.Empty)
            {
                throw new ArgumentException("Invalid command id", nameof(requestCommand));
            }

            var tcs = new TaskCompletionSource<Command>();
            if (!_pendingCommandsDictionary.TryAdd(requestCommand.Id, tcs))
            {
                throw new InvalidOperationException("Could not register the pending command request. The command id is already in use.");
            }
            try
            {
                using (cancellationToken.Register(() => tcs.TrySetCanceled()))
                {
                    await SendCommandAsync(requestCommand).ConfigureAwait(false);
                    var result = await tcs.Task.ConfigureAwait(false);
                    return result;
                }
            }
            finally
            {
                TaskCompletionSource<Command> removedTcs;
                _pendingCommandsDictionary.TryRemove(requestCommand.Id, out removedTcs);
            }                        
        }

        #endregion

        #region INotificationChannel Members

        /// <summary>
        /// Sends a notification to the remote node.
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">notification</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public virtual Task SendNotificationAsync(Notification notification) => SendAsync(notification, NotificationModules, CancellationToken.None);

        /// <summary>
        /// Receives a notification from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual Task<Notification> ReceiveNotificationAsync(CancellationToken cancellationToken) 
            => ReceiveEnvelopeAsync(_notificationBuffer, cancellationToken);


        #endregion

        #region ISessionChannel Members

        /// <summary>
        /// Sends a session change message to the remote node. 
        /// Avoid to use this method directly. Instead, use the Server or Client channel methods.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">session</exception>
        public virtual Task SendSessionAsync(Session session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (State == SessionState.Finished || State == SessionState.Failed)
            {
                throw new InvalidOperationException($"Cannot send a session in the '{State}' session state");
            }

            return SendAsync(session, CancellationToken.None);
        }

        /// <summary>
        /// Receives a session from the remote node.
        /// Avoid to use this method directly. Instead, use the Server or Client channel methods.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public virtual async Task<Session> ReceiveSessionAsync(CancellationToken cancellationToken)
        {
            switch (State)
            {
                case SessionState.Finished:
                    throw new InvalidOperationException($"Cannot receive a session in the '{State}' session state");
                case SessionState.Established:                    
                    return await ReceiveEnvelopeAsync(_sessionBuffer, cancellationToken).ConfigureAwait(false);
            }

            var result = await ReceiveAsync(cancellationToken).ConfigureAwait(false);
            var session = result as Session;
            if (session != null) return session;

            await CloseTransportAsync().ConfigureAwait(false);
            throw new InvalidOperationException("A null or unexpected envelope was received from the transport");
        }

        #endregion

        #region Protected Methods  

        protected async Task CloseTransportAsync()
        {
            if (Transport.IsConnected)
            {
                using (var cts = new CancellationTokenSource(_closeTimeout))
                {
                    await Transport.CloseAsync(cts.Token).ConfigureAwait(false);
                }
            }
        }

        #endregion

        #region Private Methods       

        private async Task ConsumeTransportAsync()
        {
            try
            {
                while (IsChannelEstablished())
                {
                    try
                    {
                        var envelope = await ReceiveAsync(_consumerCts.Token).ConfigureAwait(false);
                        if (envelope == null) continue;

                        await ConsumeEnvelopeAsync(envelope, _consumerCts.Token);
                    }
                    catch (OperationCanceledException) when (_consumerCts.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (ObjectDisposedException) when (_isDisposing)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        _isConsumeTransportTaskFaulting = true;
                        await CloseTransportAsync().ConfigureAwait(false);
                        throw;
                    }   
                }
            }
            finally
            {
                if (!_consumerCts.IsCancellationRequested)
                {
                    _consumerCts.Cancel();
                }
            }
        }

        private bool IsChannelEstablished()
        {
            return
                !_consumerCts.IsCancellationRequested &&
                State == SessionState.Established &&
                Transport.IsConnected;
        }

        /// <summary>
        /// Cancels the token that is associated to the channel send and receive tasks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Transport_Closing(object sender, DeferralEventArgs e)
        {
            using (e.GetDeferral())
            {
                if (!_consumerCts.IsCancellationRequested)
                {
                    _consumerCts.Cancel();
                }
            }
        }

        private async Task ConsumeEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            
            if (envelope is Notification)
            {
                await ConsumeNotificationAsync((Notification)envelope, cancellationToken).ConfigureAwait(false);
            }
            else if (envelope is Message)
            {
                await ConsumeMessageAsync((Message)envelope, cancellationToken).ConfigureAwait(false);
            }
            else if (envelope is Command)
            {
                await ConsumeCommandAsync((Command)envelope, cancellationToken).ConfigureAwait(false);
            }
            else if (envelope is Session)
            {
                await ConsumeSessionAsync((Session)envelope, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException("Invalid or unknown envelope received by the transport.");
            }
        }

        private Task ConsumeMessageAsync(Message message, CancellationToken cancellationToken) => ConsumeEnvelopeAsync(message, _messageBuffer, MessageModules, cancellationToken);

        private async Task ConsumeCommandAsync(Command command, CancellationToken cancellationToken)
        {
            command = await OnReceivingAsync(command, CommandModules, cancellationToken).ConfigureAwait(false);

            if (command != null)
            {
                TaskCompletionSource<Command> pendingCommand;
                if (command.Id != Guid.Empty && 
                    command.Status != CommandStatus.Pending &&
                    command.Method != CommandMethod.Observe &&
                    _pendingCommandsDictionary.TryRemove(command.Id, out pendingCommand))
                {
                    // Asychronously set the result
                    Task.Run(() => pendingCommand.TrySetResult(command), cancellationToken);
                }
                else
                {
                    await ConsumeEnvelopeAsync(command, _commandBuffer, cancellationToken).ConfigureAwait(false);
                }
            }
        } 

        private Task ConsumeNotificationAsync(Notification notification, CancellationToken cancellationToken) => ConsumeEnvelopeAsync(notification, _notificationBuffer, NotificationModules, cancellationToken);

        private Task ConsumeSessionAsync(Session session, CancellationToken cancellationToken)
        {
            if (!_sessionBuffer.Post(session))
            {
                throw new InvalidOperationException("Session buffer limit reached");
            }

            return Task.FromResult<object>(null);
        }

        private async Task ConsumeEnvelopeAsync<T>(T envelope, BufferBlock<T> buffer, IEnumerable<IChannelModule<T>> modules, CancellationToken cancellationToken) where T : Envelope, new()
        {
            envelope = await OnReceivingAsync(envelope, modules, cancellationToken);
            await ConsumeEnvelopeAsync(envelope, buffer, cancellationToken);
        }

        private async Task<T> OnReceivingAsync<T>(T envelope, IEnumerable<IChannelModule<T>> modules, CancellationToken cancellationToken) where T : Envelope, new()
        {
            foreach (var module in modules.ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (envelope == null) break;
                envelope = await module.OnReceivingAsync(envelope, cancellationToken);
            }

            return envelope;
        }

        private async Task ConsumeEnvelopeAsync<T>(T envelope, BufferBlock<T> buffer, CancellationToken cancellationToken) where T : Envelope, new()
        {
            if (envelope != null)
            {
                if (!await buffer.SendAsync(envelope, cancellationToken))
                {
                    throw new InvalidOperationException($"{typeof(T).Name} buffer limit reached");
                }
            }
        }

        /// <summary>
        /// Sends the envelope to the transport.
        /// </summary>
        /// <param name="envelope">The envelope.</param>
        /// <param name="modules"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task SendAsync<T>(T envelope, IEnumerable<IChannelModule<T>> modules, CancellationToken cancellationToken) where T : Envelope, new()
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (State != SessionState.Established)
            {
                throw new InvalidOperationException($"Cannot send a {typeof(T).Name} in the '{State}' session state");
            }

            foreach (var module in modules.ToList())
            {
                if (envelope == null) break;
                cancellationToken.ThrowIfCancellationRequested();
                envelope = await module.OnSendingAsync(envelope, cancellationToken);
            }

            if (envelope != null)
            {
                await SendAsync(envelope, cancellationToken);
            }
        }

        /// <summary>
        /// Sends the envelope to the transport.
        /// </summary>
        /// <param name="envelope">The envelope.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task SendAsync<T>(T envelope, CancellationToken cancellationToken) where T : Envelope, new()
        {
            if (!Transport.IsConnected)
            {
                throw new InvalidOperationException("The transport is not connected");
            }

            using (var timeoutCancellationTokenSource = new CancellationTokenSource(_sendTimeout))
            {
                using (var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, timeoutCancellationTokenSource.Token))
                {
                    await Transport.SendAsync(envelope, linkedCancellationTokenSource.Token).ConfigureAwait(false);
                }
            }            
        }

        /// <summary>
        /// Receives an envelope from the transport.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            return Transport.ReceiveAsync(cancellationToken);                      
        }

        private async Task<T> ReceiveEnvelopeAsync<T>(BufferBlock<T> buffer, CancellationToken cancellationToken) where T : Envelope, new()
        {            
            if (State != SessionState.Established ||                 
                _isConsumeTransportTaskFaulting ||
                _consumeTransportTask.IsFaulted)
            {
                T envelope;
                if (buffer.TryReceive(out envelope))
                {
                    return envelope;
                }

                if (State != SessionState.Established)
                {
                    throw new InvalidOperationException($"Cannot receive more envelopes in the '{State}' session state");

                }
                await _consumeTransportTask;
            }

            try
            {
                return await ReceiveFromBufferAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (_isConsumeTransportTaskFaulting) await _consumeTransportTask;
                throw;
            }
        }

        /// <summary>
        /// Receives an envelope from the buffer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<T> ReceiveFromBufferAsync<T>(ISourceBlock<T> buffer, CancellationToken cancellationToken) where T : Envelope, new()
        {
            using (var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                _consumerCts.Token, cancellationToken))
            {
                return await buffer.ReceiveAsync(linkedCancellationTokenSource.Token).ConfigureAwait(false);
            }
        }

        private static void OnStateChanged<T>(IEnumerable<IChannelModule<T>> modules, SessionState state) where T : Envelope, new()
        {
            foreach (var module in modules.ToList())
            {
                module.OnStateChanged(state);
            }
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
                _isDisposing = true;

                if (!_consumerCts.IsCancellationRequested)
                {
                    _consumerCts.Cancel();
                }

                _consumerCts.Dispose();
                Transport.DisposeIfDisposable();
            }
        }

        #endregion
    }
}
