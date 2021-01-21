using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Lime.Protocol.Network.Modules;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Base class for the protocol communication channels.
    /// </summary>
    public abstract class ChannelBase : IChannel, IDisposable
    {
        private static readonly TimeSpan ExceptionHandlerTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

        private readonly TimeSpan _closeTimeout;
        private readonly ReceiverChannel _receiverChannel;
        private readonly SenderChannel _senderChannel;
        private readonly IChannelCommandProcessor _channelCommandProcessor;
        private readonly IDisposable _remotePingChannelModule;

        private SessionState _state;
        private bool _closeTransportInvoked;

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
            if (closeTimeout == default) throw new ArgumentException("Invalid close timeout", nameof(closeTimeout));
            if (envelopeBufferSize <= 0)
            {
                envelopeBufferSize = -1;
            }
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
            Transport.Closing += Transport_Closing;
            _closeTimeout = closeTimeout;
            _channelCommandProcessor = channelCommandProcessor ?? new ChannelCommandProcessor();

            // Modules
            MessageModules = new List<IChannelModule<Message>>();
            NotificationModules = new List<IChannelModule<Notification>>();
            CommandModules = new List<IChannelModule<Command>>();
            if (autoReplyPings) CommandModules.Add(new ReplyPingChannelModule(this));
            if (fillEnvelopeRecipients) FillEnvelopeRecipientsChannelModule.CreateAndRegister(this);
            if (remotePingInterval != null)
            {
                _remotePingChannelModule = RemotePingChannelModule.CreateAndRegister(this, remotePingInterval.Value, remoteIdleTimeout);
            }

            _receiverChannel = new ReceiverChannel(
                this,
                Transport,
                _channelCommandProcessor,
                MessageModules,
                NotificationModules,
                CommandModules,
                HandleConsumerExceptionAsync,
                envelopeBufferSize,
                consumeTimeout,
                _closeTimeout);

            _senderChannel = new SenderChannel(
                this,
                Transport,
                MessageModules,
                NotificationModules,
                CommandModules,
                HandleSenderExceptionAsync,
                envelopeBufferSize,
                sendTimeout,
                _closeTimeout);
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
                    _senderChannel.Start();
                    _receiverChannel.Start();
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
        public virtual Task SendMessageAsync(Message message, CancellationToken cancellationToken)
            => _senderChannel.SendMessageAsync(message, cancellationToken);

        /// <inheritdoc />
        public virtual Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
            => _receiverChannel.ReceiveMessageAsync(cancellationToken);

        /// <inheritdoc />
        public virtual Task SendCommandAsync(Command command, CancellationToken cancellationToken)
            => _senderChannel.SendCommandAsync(command, cancellationToken);

        /// <inheritdoc />
        public virtual Task<Command> ReceiveCommandAsync(CancellationToken cancellationToken)
            => _receiverChannel.ReceiveCommandAsync(cancellationToken);

        /// <inheritdoc />
        public virtual Task<Command> ProcessCommandAsync(Command requestCommand, CancellationToken cancellationToken)
            => _channelCommandProcessor.ProcessCommandAsync(this, requestCommand, cancellationToken);

        /// <inheritdoc />
        public virtual Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken)
            => _senderChannel.SendNotificationAsync(notification, cancellationToken);

        /// <inheritdoc />
        public virtual Task<Notification> ReceiveNotificationAsync(CancellationToken cancellationToken)
            => _receiverChannel.ReceiveNotificationAsync(cancellationToken);

        /// <inheritdoc />
        public virtual Task SendSessionAsync(Session session, CancellationToken cancellationToken)
            => _senderChannel.SendSessionAsync(session, cancellationToken);

        /// <inheritdoc />
        public virtual Task<Session> ReceiveSessionAsync(CancellationToken cancellationToken)
            => _receiverChannel.ReceiveSessionAsync(cancellationToken);

        /// <summary>
        /// Closes the underlying transport.
        /// </summary>
        /// <returns></returns>
        protected async Task CloseTransportAsync()
        {
            _closeTransportInvoked = true;

            try
            {
                await StopChannelTasks().ConfigureAwait(false);
            }
            finally
            {
                if (Transport.IsConnected)
                {
                    using var cts = new CancellationTokenSource(_closeTimeout);
                    await Transport.CloseAsync(cts.Token).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Stops the sender and receiver tasks.
        /// </summary>
        /// <returns></returns>
        private async Task StopChannelTasks()
        {
            using var cts = new CancellationTokenSource(StopTimeout);
            await Task.WhenAll(
                _receiverChannel.StopAsync(cts.Token),
                _senderChannel.StopAsync(cts.Token));
        }

        /// <summary>
        /// Cancels the token that is associated to the channel send and receive tasks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Transport_Closing(object sender, DeferralEventArgs e)
        {
            if (_closeTransportInvoked) return;

            try
            {
                using (e.GetDeferral())
                {
                    await StopChannelTasks().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("[ChannelBase] Unhandled exception in the Transport_Closing handler: {0}", ex);
            }
        }

        private static void OnStateChanged<T>(IEnumerable<IChannelModule<T>> modules, SessionState state) where T : Envelope, new()
        {
            foreach (var module in modules.ToList())
            {
                module.OnStateChanged(state);
            }
        }

        private async Task HandleConsumerExceptionAsync(Exception exception) => await HandleExceptionAndCloseAsync(exception, ConsumerException);

        private async Task HandleSenderExceptionAsync(Exception exception)
        {
            if (exception is EnvelopeTooLargeException)
            {
                await RaiseExceptionHandlerAsync(SenderException, exception).ConfigureAwait(false);
            }
            else
            {
                await HandleExceptionAndCloseAsync(exception, SenderException);
            }
        }

        private async Task HandleExceptionAndCloseAsync(Exception exception, EventHandler<ExceptionEventArgs> handler)
        {
            await RaiseExceptionHandlerAsync(handler, exception).ConfigureAwait(false);
            await CloseTransportAsync().ConfigureAwait(false);
        }

        private async Task RaiseExceptionHandlerAsync(EventHandler<ExceptionEventArgs> handler, Exception exception)
        {
            try
            {
                if (handler == null) return;
                using var cts = new CancellationTokenSource(ExceptionHandlerTimeout);
                var args = new ExceptionEventArgs(exception);
                handler.Invoke(this, args);
                await args.WaitForDeferralsAsync(cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // The exception handler is expected to never fail
                Trace.TraceError("Channel {0} exception handler failed: {1}", SessionId, ex);
            }
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
                _receiverChannel.Dispose();
                _senderChannel.Dispose();
                Transport.DisposeIfDisposable();
                _remotePingChannelModule?.Dispose();
            }
        }
    }
}
