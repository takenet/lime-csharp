using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Network.Modules;
using System.Text;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Base class for the protocol communication channels.
    /// </summary>
    public abstract class ChannelBase : IChannel, IDisposable
    {
        private static readonly ExecutionDataflowBlockOptions SingleBoundedCapacityBlockOptions = new ExecutionDataflowBlockOptions() { BoundedCapacity = 1 };
        private static readonly DataflowLinkOptions PropagateCompletionLinkOptions = new DataflowLinkOptions() { PropagateCompletion = true };

        private readonly TimeSpan _sendTimeout;
        private readonly TimeSpan? _consumeTimeout;
        private readonly TimeSpan _closeTimeout;
        private readonly BufferBlock<Envelope> _transportBuffer;
        private readonly TransformBlock<Envelope, Message> _messageConsumerBlock;
        private readonly TransformBlock<Envelope, Command> _commandConsumerBlock;
        private readonly TransformBlock<Envelope, Notification> _notificationConsumerBlock;
        private readonly TransformBlock<Envelope, Session> _sessionConsumerBlock;
        private readonly BufferBlock<Message> _messageBuffer;
        private readonly BufferBlock<Command> _commandBuffer;
        private readonly BufferBlock<Notification> _notificationBuffer;
        private readonly BufferBlock<Session> _sessionBuffer;
        private readonly ITargetBlock<Envelope> _drainEnvelopeBlock;
        private readonly IChannelCommandProcessor _channelCommandProcessor;
        private readonly CancellationTokenSource _consumerCts;
        private readonly object _syncRoot;
        private SessionState _state;
        private Task _consumeTransportTask;
        private Exception _consumerTransportException;
        private bool _isDisposing;

        /// <summary>
        /// Creates a new instance of ChannelBase
        /// </summary>
        /// <param name="transport">The transport.</param>
        /// <param name="sendTimeout">The channel send timeout.</param>
        /// <param name="consumeTimeout">The channel consume timeout. Each envelope received from the transport must be consumed in the specified timeout or it will cause the channel to be closed.</param>
        /// <param name="closeTimeout">The channel close timeout.</param>
        /// <param name="envelopeBufferSize">Size of the envelope buffer.</param>
        /// <param name="fillEnvelopeRecipients">Indicates if the from and to properties of sent and received envelopes should be filled with the session information if not defined.</param>
        /// <param name="autoReplyPings">Indicates if the channel should reply automatically to ping request commands. In this case, the ping command are not returned by the ReceiveCommandAsync method.</param>
        /// <param name="remotePingInterval">The interval to ping the remote party.</param>
        /// <param name="remoteIdleTimeout">The timeout to close the channel due to inactivity.</param>
        /// <param name="channelCommandProcessor">The channel command processor.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException">
        /// Invalid send timeout
        /// or
        /// Invalid consume timeout
        /// or
        /// Invalid close timeout
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        protected ChannelBase(
            ITransport transport,
            TimeSpan sendTimeout,
            TimeSpan? consumeTimeout,
            TimeSpan closeTimeout,
            int envelopeBufferSize,
            bool fillEnvelopeRecipients,
            bool autoReplyPings,
            TimeSpan? remotePingInterval,
            TimeSpan? remoteIdleTimeout,
            IChannelCommandProcessor channelCommandProcessor)
        {
            if (sendTimeout == default(TimeSpan)) throw new ArgumentException("Invalid send timeout", nameof(sendTimeout));
            if (consumeTimeout != null && consumeTimeout.Value == default(TimeSpan)) throw new ArgumentException("Invalid consume timeout", nameof(consumeTimeout));
            if (closeTimeout == default(TimeSpan)) throw new ArgumentException("Invalid close timeout", nameof(closeTimeout));
            if (envelopeBufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(envelopeBufferSize));

            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
            Transport.Closing += Transport_Closing;
            _sendTimeout = sendTimeout;
            _consumeTimeout = consumeTimeout;
            _closeTimeout = closeTimeout;
            _consumerCts = new CancellationTokenSource();
            _syncRoot = new object();
            _transportBuffer = new BufferBlock<Envelope>(SingleBoundedCapacityBlockOptions);        
            _messageConsumerBlock = new TransformBlock<Envelope, Message>(e => ConsumeMessageAsync(e), SingleBoundedCapacityBlockOptions);
            _commandConsumerBlock = new TransformBlock<Envelope, Command>(e => ConsumeCommandAsync(e), SingleBoundedCapacityBlockOptions);
            _notificationConsumerBlock = new TransformBlock<Envelope, Notification>(e => ConsumeNotificationAsync(e), SingleBoundedCapacityBlockOptions);
            _sessionConsumerBlock = new TransformBlock<Envelope, Session>(e => ConsumeSession(e), SingleBoundedCapacityBlockOptions);
            var options = new DataflowBlockOptions() { BoundedCapacity = envelopeBufferSize };
            _messageBuffer = new BufferBlock<Message>(options);
            _commandBuffer = new BufferBlock<Command>(options);
            _notificationBuffer = new BufferBlock<Notification>(options);
            _sessionBuffer = new BufferBlock<Session>(SingleBoundedCapacityBlockOptions);
            _drainEnvelopeBlock = DataflowBlock.NullTarget<Envelope>();
            _transportBuffer.LinkTo(_messageConsumerBlock, PropagateCompletionLinkOptions, e => e is Message);
            _transportBuffer.LinkTo(_commandConsumerBlock, PropagateCompletionLinkOptions, e => e is Command);
            _transportBuffer.LinkTo(_notificationConsumerBlock, PropagateCompletionLinkOptions, e => e is Notification);
            _transportBuffer.LinkTo(_sessionConsumerBlock, PropagateCompletionLinkOptions, e => e is Session);
            _messageConsumerBlock.LinkTo(_messageBuffer, PropagateCompletionLinkOptions, e => e != null);
            _messageConsumerBlock.LinkTo(_drainEnvelopeBlock, e => e == null);
            _commandConsumerBlock.LinkTo(_commandBuffer, PropagateCompletionLinkOptions, e => e != null);
            _commandConsumerBlock.LinkTo(_drainEnvelopeBlock, e => e == null);
            _notificationConsumerBlock.LinkTo(_notificationBuffer, PropagateCompletionLinkOptions, e => e != null);
            _notificationConsumerBlock.LinkTo(_drainEnvelopeBlock, e => e == null);
            _sessionConsumerBlock.LinkTo(_sessionBuffer, PropagateCompletionLinkOptions, e => e != null);
            _sessionConsumerBlock.LinkTo(_drainEnvelopeBlock, e => e == null);
            _channelCommandProcessor = channelCommandProcessor ?? new ChannelCommandProcessor();
            MessageModules = new List<IChannelModule<Message>>();
            NotificationModules = new List<IChannelModule<Notification>>();
            CommandModules = new List<IChannelModule<Command>>();

            if (autoReplyPings) CommandModules.Add(new ReplyPingChannelModule(this));
            if (fillEnvelopeRecipients) FillEnvelopeRecipientsChannelModule.CreateAndRegister(this);
            if (remotePingInterval != null) RemotePingChannelModule.CreateAndRegister(this, remotePingInterval.Value, remoteIdleTimeout);
        }

        ~ChannelBase()
        {
            Dispose(false);
        }

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
        public string SessionId { get; protected set; }

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

        /// <summary>
        /// Occurs when the channel consumer task throws an exception.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ConsumerException;

        /// <summary>
        /// Sends a message to the remote node.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">message</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public virtual Task SendMessageAsync(Message message, CancellationToken cancellationToken)
            => SendAsync(message, cancellationToken, MessageModules);
        
        /// <summary>
        /// Receives a message from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public virtual Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
            => ReceiveFromBufferAsync(_messageBuffer, cancellationToken);

        /// <summary>
        /// Sends a command envelope to the remote node.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">message</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public virtual Task SendCommandAsync(Command command, CancellationToken cancellationToken)
            => SendAsync(command, cancellationToken, CommandModules);

        /// <summary>
        /// Receives a command from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual Task<Command> ReceiveCommandAsync(CancellationToken cancellationToken)
            => ReceiveFromBufferAsync(_commandBuffer, cancellationToken);

        /// <summary>
        /// Processes the command request.
        /// </summary>
        /// <param name="requestCommand">The request command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual Task<Command> ProcessCommandAsync(Command requestCommand, CancellationToken cancellationToken)
            => _channelCommandProcessor.ProcessCommandAsync(this, requestCommand, cancellationToken);

        /// <summary>
        /// Sends a notification to the remote node.
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">notification</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public virtual Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken)
            => SendAsync(notification, cancellationToken, NotificationModules);

        /// <summary>
        /// Receives a notification from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual Task<Notification> ReceiveNotificationAsync(CancellationToken cancellationToken)
            => ReceiveFromBufferAsync(_notificationBuffer, cancellationToken);

        /// <summary>
        /// Sends a session change message to the remote node. 
        /// Avoid to use this method directly. Instead, use the Server or Client channel methods.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">session</exception>
        public virtual Task SendSessionAsync(Session session, CancellationToken cancellationToken)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (State == SessionState.Finished || State == SessionState.Failed)
            {
                throw new InvalidOperationException($"Cannot send a session in the '{State}' session state");
            }

            return SendAsync(session, cancellationToken);
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
                    return await ReceiveFromBufferAsync(_sessionBuffer, cancellationToken).ConfigureAwait(false);
            }

            var result = await ReceiveFromTransportAsync(cancellationToken).ConfigureAwait(false);
            var session = result as Session;
            if (session != null) return session;

            await CloseTransportAsync().ConfigureAwait(false);
            throw new InvalidOperationException("An empty or unexpected envelope was received from the transport");
        }

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

        private bool IsChannelEstablished()
            => !_consumerCts.IsCancellationRequested
                && State == SessionState.Established
                && Transport.IsConnected;

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

        private async Task ConsumeTransportAsync()
        {
            try
            {
                while (IsChannelEstablished())
                {
                    try
                    {
                        var envelope = await ReceiveFromTransportAsync(_consumerCts.Token).ConfigureAwait(false);
                        if (envelope == null) continue;

                        using (var timeoutCts = _consumeTimeout == null ? new CancellationTokenSource() : new CancellationTokenSource(_consumeTimeout.Value))
                        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, _consumerCts.Token))
                        {
                            try
                            {
                                if (!await _transportBuffer.SendAsync(envelope, linkedCts.Token))
                                {
                                    throw new InvalidOperationException("Transport buffer limit reached");
                                }
                            }
                            catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && _consumeTimeout != null)
                            {
                                var exceptionMessageBuilder = new StringBuilder($"The transport consumer has timed out after {_consumeTimeout.Value.TotalSeconds} seconds.");
                                if (_messageBuffer.Count > 0
                                    || _notificationBuffer.Count > 0
                                    || _commandBuffer.Count > 0
                                    || _sessionBuffer.Count > 0)
                                {
                                    exceptionMessageBuilder.Append(
                                        $" The receiver buffer has {_messageBuffer.Count} ({_messageConsumerBlock.InputCount}/{_messageConsumerBlock.OutputCount}) messages,");
                                    exceptionMessageBuilder.Append(
                                        $" {_notificationBuffer.Count} ({_notificationConsumerBlock.InputCount}/{_notificationConsumerBlock.OutputCount}) notifications,");
                                    exceptionMessageBuilder.Append(
                                        $" {_commandBuffer.Count} ({_commandConsumerBlock.InputCount}/{_commandConsumerBlock.OutputCount}) commands,");
                                    exceptionMessageBuilder.Append(
                                        $" and {_sessionBuffer.Count} sessions and it may be the cause of the problem. Please ensure that the channel receive methods are being called.");
                                }

                                throw new TimeoutException(exceptionMessageBuilder.ToString(), ex);
                            }
                        }
                    }
                    catch (OperationCanceledException) when (_consumerCts.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (ObjectDisposedException) when (_isDisposing)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        await RaiseConsumerExceptionAsync(ex);

                        await CloseTransportAsync().ConfigureAwait(false);
                        throw;
                    }
                }
            }
            finally
            {
                _transportBuffer.Complete();
                _channelCommandProcessor.CancelAll();
                if (!_consumerCts.IsCancellationRequested) _consumerCts.Cancel();
            }

            if (_consumerTransportException != null) throw _consumerTransportException;
        }



        private Task<Message> ConsumeMessageAsync(Envelope envelope) => OnReceivingAsync((Message)envelope, MessageModules, _consumerCts.Token);

        private async Task<Command> ConsumeCommandAsync(Envelope envelope)
        {
            var command = await OnReceivingAsync((Command)envelope, CommandModules, _consumerCts.Token);;

            try
            {
                if (command != null &&
                    !_channelCommandProcessor.TrySubmitCommandResult(command))
                {
                    return command;
                }
            }
            catch (OperationCanceledException) when (_consumerCts.IsCancellationRequested) { }
            catch (Exception ex)
            {
                await RaiseConsumerExceptionAsync(ex);
                _consumerTransportException = ex;
                _consumerCts.Cancel();
            }
            return null;
        }

        private Task<Notification> ConsumeNotificationAsync(Envelope envelope)
            => OnReceivingAsync((Notification)envelope, NotificationModules, _consumerCts.Token);

        private Session ConsumeSession(Envelope envelope) => (Session) envelope;

        private async Task<T> OnReceivingAsync<T>(T envelope, IEnumerable<IChannelModule<T>> modules, CancellationToken cancellationToken) where T : Envelope, new()
        {
            try
            {
                foreach (var module in modules.ToList())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (envelope == null) break;
                    envelope = await module.OnReceivingAsync(envelope, cancellationToken);
                }

                return envelope;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                await RaiseConsumerExceptionAsync(ex);
                _consumerTransportException = ex;
                _consumerCts.Cancel();
            }

            return null;
        }

        /// <summary>
        /// Sends the envelope to the transport.
        /// </summary>
        /// <param name="envelope">The envelope.</param>
        /// <param name="cancellationToken"></param>
        /// <param name="modules"></param>
        /// <returns></returns>
        private async Task SendAsync<T>(T envelope, CancellationToken cancellationToken, IEnumerable<IChannelModule<T>> modules) where T : Envelope, new()
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
            using (var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCancellationTokenSource.Token))
            {
                await Transport.SendAsync(envelope, linkedCancellationTokenSource.Token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Receives an envelope from the transport.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private Task<Envelope> ReceiveFromTransportAsync(CancellationToken cancellationToken) 
            => Transport.ReceiveAsync(cancellationToken);

        /// <summary>
        /// Receives an envelope from the buffer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer">The buffer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        private async Task<T> ReceiveFromBufferAsync<T>(ISourceBlock<T> buffer, CancellationToken cancellationToken) where T : Envelope, new()
        {
            if (State < SessionState.Established)
            {
                throw new InvalidOperationException($"Cannot receive envelopes in the '{State}' session state");
            }

            try
            {
                return await buffer.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex) when (buffer.Completion.IsCompleted)
            {
                if (_consumeTransportTask != null)
                {
                    await _consumeTransportTask.ConfigureAwait(false);
                    _consumeTransportTask = null;
                }

                throw new InvalidOperationException("The channel listener task is complete and cannot receive envelopes", ex);
            }
        }

        private static void OnStateChanged<T>(IEnumerable<IChannelModule<T>> modules, SessionState state) where T : Envelope, new()
        {
            foreach (var module in modules.ToList())
            {
                module.OnStateChanged(state);
            }
        }

        private async Task RaiseConsumerExceptionAsync(Exception ex)
        {
            var args = new ExceptionEventArgs(ex);
            ConsumerException.RaiseEvent(this, new ExceptionEventArgs(ex));
            await args.WaitForDeferralsAsync(_consumerCts.Token).ConfigureAwait(false);
        }

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
    }
}
