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
        private static readonly DataflowLinkOptions PropagateCompletionLinkOptions = new DataflowLinkOptions() { PropagateCompletion = true };

        private readonly TimeSpan _sendTimeout;
        private readonly TimeSpan? _consumeTimeout;
        private readonly TimeSpan _closeTimeout;
        private readonly CancellationTokenSource _consumerCts;
        private readonly BufferBlock<Envelope> _receiveEnvelopeBuffer;
        private readonly TransformBlock<Envelope, Message> _messageConsumerBlock;
        private readonly TransformBlock<Envelope, Command> _commandConsumerBlock;
        private readonly TransformBlock<Envelope, Notification> _notificationConsumerBlock;
        private readonly TransformBlock<Envelope, Session> _sessionConsumerBlock;
        private readonly BufferBlock<Message> _receiveMessageBuffer;
        private readonly BufferBlock<Command> _receiveCommandBuffer;
        private readonly BufferBlock<Notification> _receiveNotificationBuffer;
        private readonly BufferBlock<Session> _receiveSessionBuffer;
        private readonly ITargetBlock<Envelope> _drainEnvelopeBlock;
        private readonly CancellationTokenSource _senderCts;
        private readonly ActionBlock<(Envelope, TaskCompletionSource<Envelope>)> _sendEnvelopeBlock;
        private readonly IChannelCommandProcessor _channelCommandProcessor;
        
        private readonly object _syncRoot;
        private SessionState _state;
        private Task _consumeTransportTask;
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
            if (sendTimeout == default) throw new ArgumentException("Invalid send timeout", nameof(sendTimeout));
            if (consumeTimeout != null && consumeTimeout.Value == default) throw new ArgumentException("Invalid consume timeout", nameof(consumeTimeout));
            if (closeTimeout == default) throw new ArgumentException("Invalid close timeout", nameof(closeTimeout));
            if (envelopeBufferSize <= 0)
            {
                envelopeBufferSize = DataflowBlockOptions.Unbounded;
            }
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
            Transport.Closing += Transport_Closing;
            _sendTimeout = sendTimeout;
            _consumeTimeout = consumeTimeout;
            _closeTimeout = closeTimeout;
            _channelCommandProcessor = channelCommandProcessor ?? new ChannelCommandProcessor();
            _syncRoot = new object();
            
            // Receive pipeline
            _consumerCts = new CancellationTokenSource();
            var consumerDataflowBlockOptions = new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = envelopeBufferSize,
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                EnsureOrdered = false
            };
            _receiveEnvelopeBuffer = new BufferBlock<Envelope>(consumerDataflowBlockOptions);        
            _messageConsumerBlock = new TransformBlock<Envelope, Message>(e => ConsumeMessageAsync(e), consumerDataflowBlockOptions);
            _commandConsumerBlock = new TransformBlock<Envelope, Command>(e => ConsumeCommandAsync(e), consumerDataflowBlockOptions);
            _notificationConsumerBlock = new TransformBlock<Envelope, Notification>(e => ConsumeNotificationAsync(e), consumerDataflowBlockOptions);
            _sessionConsumerBlock = new TransformBlock<Envelope, Session>(e => ConsumeSession(e), consumerDataflowBlockOptions);
            _receiveMessageBuffer = new BufferBlock<Message>(consumerDataflowBlockOptions);
            _receiveCommandBuffer = new BufferBlock<Command>(consumerDataflowBlockOptions);
            _receiveNotificationBuffer = new BufferBlock<Notification>(consumerDataflowBlockOptions);
            _receiveSessionBuffer = new BufferBlock<Session>(consumerDataflowBlockOptions);
            _drainEnvelopeBlock = DataflowBlock.NullTarget<Envelope>();
            _receiveEnvelopeBuffer.LinkTo(_messageConsumerBlock, PropagateCompletionLinkOptions, e => e is Message);
            _receiveEnvelopeBuffer.LinkTo(_commandConsumerBlock, PropagateCompletionLinkOptions, e => e is Command);
            _receiveEnvelopeBuffer.LinkTo(_notificationConsumerBlock, PropagateCompletionLinkOptions, e => e is Notification);
            _receiveEnvelopeBuffer.LinkTo(_sessionConsumerBlock, PropagateCompletionLinkOptions, e => e is Session);
            _messageConsumerBlock.LinkTo(_receiveMessageBuffer, PropagateCompletionLinkOptions, e => e != null);
            _messageConsumerBlock.LinkTo(_drainEnvelopeBlock, e => e == null);
            _commandConsumerBlock.LinkTo(_receiveCommandBuffer, PropagateCompletionLinkOptions, e => e != null);
            _commandConsumerBlock.LinkTo(_drainEnvelopeBlock, e => e == null);
            _notificationConsumerBlock.LinkTo(_receiveNotificationBuffer, PropagateCompletionLinkOptions, e => e != null);
            _notificationConsumerBlock.LinkTo(_drainEnvelopeBlock, e => e == null);
            _sessionConsumerBlock.LinkTo(_receiveSessionBuffer, PropagateCompletionLinkOptions, e => e != null);
            _sessionConsumerBlock.LinkTo(_drainEnvelopeBlock, e => e == null);
            
            // Send pipeline
            _senderCts = new CancellationTokenSource();
            _sendEnvelopeBlock = new ActionBlock<(Envelope Envelope, TaskCompletionSource<Envelope> SentTcs)>(
                e => SendToTransportAsync(e.Envelope, e.SentTcs), 
                new ExecutionDataflowBlockOptions()
                {
                    BoundedCapacity = envelopeBufferSize,
                    MaxDegreeOfParallelism = 1,
                    EnsureOrdered = true,
                });
            
            // Modules
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
            get => _state;
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

        /// <inheritdoc />
        public ICollection<IChannelModule<Message>> MessageModules { get; }

        /// <inheritdoc />
        public ICollection<IChannelModule<Notification>> NotificationModules { get; }

        /// <inheritdoc />
        public ICollection<IChannelModule<Command>> CommandModules { get; }

        /// <inheritdoc />
        public event EventHandler<ExceptionEventArgs> ConsumerException;

        /// <inheritdoc />
        public event EventHandler<ExceptionEventArgs> SenderException;

        /// <inheritdoc />
        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_sendEnvelopeBlock.InputCount == 0 || 
                _sendEnvelopeBlock.Completion.IsCompleted)
            {
                return;
            }
            
            var sentTcs = new TaskCompletionSource<Envelope>();
            using (cancellationToken.Register(() => sentTcs.TrySetCanceled(cancellationToken)))
            {
                // Sends a "null" message only to force the completion of the tcs by the SendToTransportAsync method.
                await SendToBufferAsync<Message>(null, cancellationToken, sentTcs);
                await sentTcs.Task.ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public virtual Task SendMessageAsync(Message message, CancellationToken cancellationToken)
            => SendAsync(message, cancellationToken, MessageModules);
        
        /// <summary>
        /// Receives a message from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public virtual Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
            => ReceiveFromBufferAsync(_receiveMessageBuffer, cancellationToken);

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
            => ReceiveFromBufferAsync(_receiveCommandBuffer, cancellationToken);

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
            => ReceiveFromBufferAsync(_receiveNotificationBuffer, cancellationToken);

        /// <summary>
        /// Sends a session change message to the remote node. 
        /// Avoid to use this method directly. Instead, use the Server or Client channel methods.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">session</exception>
        public virtual async Task SendSessionAsync(Session session, CancellationToken cancellationToken)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (State == SessionState.Finished || State == SessionState.Failed)
            {
                throw new InvalidOperationException($"Cannot send a session in the '{State}' session state");
            }

            await SendToBufferAsync(session, cancellationToken);
            await FlushAsync(cancellationToken).ConfigureAwait(false);
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
                    return await ReceiveFromBufferAsync(_receiveSessionBuffer, cancellationToken).ConfigureAwait(false);
            }

            var result = await ReceiveFromTransportAsync(cancellationToken).ConfigureAwait(false);
            if (result is Session session) return session;

            await CloseTransportAsync().ConfigureAwait(false);
            throw new InvalidOperationException("An empty or unexpected envelope was received from the transport");
        }

        protected async Task CloseTransportAsync()
        {
            if (Transport.IsConnected)
            {
                using var cts = new CancellationTokenSource(_closeTimeout);
                await Transport.CloseAsync(cts.Token).ConfigureAwait(false);
            }
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

                if (!_senderCts.IsCancellationRequested)
                {
                    _senderCts.Cancel();
                }
            }
        }

        private bool IsChannelEstablished()
            => !_consumerCts.IsCancellationRequested &&
               !_senderCts.IsCancellationRequested &&
                State == SessionState.Established &&
                Transport.IsConnected;

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

                        using var timeoutCts = _consumeTimeout == null ? new CancellationTokenSource() : new CancellationTokenSource(_consumeTimeout.Value);
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, _consumerCts.Token);
                        
                        try
                        {
                            if (!await _receiveEnvelopeBuffer.SendAsync(envelope, linkedCts.Token))
                            {
                                throw new InvalidOperationException("Transport buffer limit reached");
                            }
                        }
                        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && _consumeTimeout != null)
                        {
                            throw new TimeoutException($"The transport consumer has timed out after {_consumeTimeout.Value.TotalSeconds} seconds.", ex);
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
                        break;
                    }
                }
            }
            finally
            {
                // Complete the receive pipeline to propagate to the envelope specific buffers
                _receiveEnvelopeBuffer.Complete();
                _channelCommandProcessor.CancelAll();
                if (!_consumerCts.IsCancellationRequested) _consumerCts.Cancel();
            }
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
                _consumerCts.Cancel();
            }
            return null;
        }

        private Task<Notification> ConsumeNotificationAsync(Envelope envelope) => OnReceivingAsync((Notification)envelope, NotificationModules, _consumerCts.Token);

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
                _consumerCts.Cancel();
            }

            return null;
        }
        
        /// <summary>
        /// Receives an envelope directly from the transport.
        /// </summary>
        private Task<Envelope> ReceiveFromTransportAsync(CancellationToken cancellationToken) => Transport.ReceiveAsync(cancellationToken);

        /// <summary>
        /// Receives an envelope from the buffer.
        /// </summary>
        private async Task<T> ReceiveFromBufferAsync<T>(ISourceBlock<T> buffer, CancellationToken cancellationToken) 
            where T : Envelope, new()
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
                throw new InvalidOperationException("The channel listener task is complete and cannot receive envelopes", ex);
            }
        }
        
        /// <summary>
        /// Sends the envelope to the transport.
        /// </summary>
        private async Task SendAsync<T>(T envelope, CancellationToken cancellationToken, IEnumerable<IChannelModule<T>> modules) 
            where T : Envelope, new()
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
                await SendToBufferAsync(envelope, cancellationToken);
            }
        }

        /// <summary>
        /// Sends the envelope to the transport using the envelope buffer.
        /// </summary>
        /// <param name="envelope">The envelope instance to be sent</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <param name="sentTcs">A TaskCompletionSource that is completed after the envelope is sent to the transport</param>
        private async Task SendToBufferAsync<T>(T envelope, CancellationToken cancellationToken, TaskCompletionSource<Envelope> sentTcs = null) 
            where T : Envelope, new()
        {
            if (!Transport.IsConnected)
            {
                throw new InvalidOperationException("The transport is not connected");
            }

            if (_sendEnvelopeBlock.Completion.IsCompleted)
            {
                throw new InvalidOperationException("The send task is complete", _sendEnvelopeBlock.Completion.Exception?.GetBaseException());
            }
            
            if (!await _sendEnvelopeBlock.SendAsync((envelope, sentTcs), cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The send buffer is not accepting more envelopes");
            }
        }
        
        private async Task SendToTransportAsync(Envelope envelope, TaskCompletionSource<Envelope> sentTcs = null)
        {
            try
            {
                using var cts = new CancellationTokenSource(_sendTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_senderCts.Token, cts.Token);
                
                if (envelope != null)
                {
                    await Transport.SendAsync(envelope, linkedCts.Token).ConfigureAwait(false);
                }
                    
                sentTcs?.TrySetResult(envelope);
            }
            catch (OperationCanceledException) when (_senderCts.IsCancellationRequested)
            {
                sentTcs?.TrySetCanceled(_senderCts.Token);
            }
            catch (ObjectDisposedException) when (_isDisposing)
            {
                sentTcs?.TrySetCanceled();
            }
            catch (Exception ex)
            {
                await RaiseSenderExceptionAsync(ex);
                _sendEnvelopeBlock.Complete();
                if (!_senderCts.IsCancellationRequested) _senderCts.Cancel();
                sentTcs?.TrySetException(ex);
                await CloseTransportAsync().ConfigureAwait(false);
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
        
        private async Task RaiseSenderExceptionAsync(Exception ex)
        {
            var args = new ExceptionEventArgs(ex);
            SenderException.RaiseEvent(this, new ExceptionEventArgs(ex));
            await args.WaitForDeferralsAsync(_senderCts.Token).ConfigureAwait(false);
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

                if (!_consumerCts.IsCancellationRequested) _consumerCts.Cancel();
                if (!_senderCts.IsCancellationRequested)  _senderCts.Cancel();
                _consumerCts.Dispose();
                _senderCts.Dispose();
                Transport.DisposeIfDisposable();
            }
        }
    }
}
