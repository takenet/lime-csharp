using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;

namespace Lime.Protocol.Client
{
    /// <summary>
    /// Defines a client channel that manages the session state and connects to the server on demand.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Client.IOnDemandClientChannel" />
    /// <seealso cref="System.IDisposable" />
    /// <seealso cref="ICommandChannel" />
    /// <seealso cref="IMessageChannel" />
    /// <seealso cref="INotificationChannel" />
    public sealed class OnDemandClientChannel : IOnDemandClientChannel, IDisposable
    {
        private readonly IEstablishedClientChannelBuilder _builder;
        private readonly TimeSpan _sendTimeout;
        private readonly SemaphoreSlim _semaphore;
        private IClientChannel _clientChannel;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnDemandClientChannel"/> class.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public OnDemandClientChannel(IEstablishedClientChannelBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (builder.ChannelBuilder == null) throw new ArgumentException("The specified builder is invalid", nameof(builder));
            _builder = builder;
            _sendTimeout = builder.ChannelBuilder.SendTimeout;
            _semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Sends a command envelope to the remote node.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public Task SendCommandAsync(Command command)
        {
            return SendAsync(command, (channel, envelope) => channel.SendCommandAsync(envelope));
        }

        /// <summary>
        /// Receives a command from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public Task<Command> ReceiveCommandAsync(CancellationToken cancellationToken)
        {
            return ReceiveAsync(cancellationToken, (channel, token) => channel.ReceiveCommandAsync(token));
        }

        /// <summary>
        /// Sends a message to the remote node.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task SendMessageAsync(Message message)
        {
            return SendAsync(message, (channel, envelope) => channel.SendMessageAsync(envelope));
        }

        /// <summary>
        /// Receives a message from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            return ReceiveAsync(cancellationToken, (channel, token) => channel.ReceiveMessageAsync(token));
        }

        /// <summary>
        /// Sends a notification to the remote node.
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        public Task SendNotificationAsync(Notification notification)
        {
            return SendAsync(notification, (channel, envelope) => channel.SendNotificationAsync(envelope));
        }

        /// <summary>
        /// Receives a notification from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public Task<Notification> ReceiveNotificationAsync(CancellationToken cancellationToken)
        {
            return ReceiveAsync(cancellationToken, (channel, token) => channel.ReceiveNotificationAsync(token));
        }

        /// <summary>
        /// Gets a value indicating whether this instance has an established client channel.
        /// </summary>
        public bool IsEstablished
            => _clientChannel != null &&
            _clientChannel.State == SessionState.Established && 
            _clientChannel.Transport.IsConnected;

        /// <summary>
        /// Occurs when the channel creation failed.
        /// </summary>
        public event EventHandler<ClientChannelExceptionEventArgs> ChannelCreationFailed;

        /// <summary>
        /// Occurs when the channel send or receive action failed.
        /// </summary>
        public event EventHandler<ClientChannelExceptionEventArgs> ChannelOperationFailed;

        /// <summary>
        /// Finishes the associated client channel, if established.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task FinishClientChannelAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_clientChannel != null &&
                    _clientChannel.State == SessionState.Established &&
                    _clientChannel.Transport.IsConnected)
                {
                    var finishedSessionTask = _clientChannel.ReceiveFinishedSessionAsync(cancellationToken);
                    await _clientChannel.SendFinishingSessionAsync().ConfigureAwait(false);
                    await finishedSessionTask.ConfigureAwait(false);
                }
            }            
            finally
            {
                _semaphore.Release();
            }
        }

        private Task SendAsync<T>(T envelope, Func<IClientChannel, T, Task> sendFunc) where T : Envelope, new()
        {
            using (var cts = new CancellationTokenSource(_sendTimeout))
            {
                return SendAsync(envelope, cts.Token, sendFunc);
            }
        }

        private async Task SendAsync<T>(T envelope, CancellationToken cancellationToken, Func<IClientChannel, T, Task> sendFunc) where T : Envelope, new()
        {
            while (!_disposed)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var channel = await GetChannelAsync(cancellationToken).ConfigureAwait(false);
                try
                {                    
                    await sendFunc(channel, envelope).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
                {
                    if (!await HandleChannelOperationExceptionAsync(ex, channel, cancellationToken)) throw;
                }
            }

            throw new ObjectDisposedException(nameof(OnDemandClientChannel));
        }

        private async Task<T> ReceiveAsync<T>(CancellationToken cancellationToken, Func<IClientChannel, CancellationToken, Task<T>> receiveFunc) where T : Envelope, new()
        {
            while (!_disposed)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var channel = await GetChannelAsync(cancellationToken).ConfigureAwait(false);
                try
                {                    
                    return await receiveFunc(channel, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
                {
                    if (!await HandleChannelOperationExceptionAsync(ex, channel, cancellationToken)) throw;
                }
            }

            throw new ObjectDisposedException(nameof(OnDemandClientChannel));
        }

        private async Task<bool> HandleChannelOperationExceptionAsync(Exception ex, IClientChannel channel, CancellationToken cancellationToken)
        {
            var eventArgs = new ClientChannelExceptionEventArgs(channel.SessionId, channel.State, channel.Transport.IsConnected, true, ex);
            ChannelOperationFailed?.RaiseEvent(this, eventArgs);
            await eventArgs.WaitForDeferralsAsync().ConfigureAwait(false);

            if (channel.State != SessionState.Established || !channel.Transport.IsConnected)
            {
                await DiscardChannelAsync(channel, cancellationToken).ConfigureAwait(false);
            }

            return eventArgs.IsHandled;
        }

        private async Task<IClientChannel> GetChannelAsync(CancellationToken cancellationToken)
        {
            var clientChannel = _clientChannel;
            while (clientChannel == null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    clientChannel = _clientChannel;
                    if (clientChannel == null)
                    {
                        clientChannel = _clientChannel = await _builder
                            .BuildAndEstablishAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
                {
                    var eventArgs = new ClientChannelExceptionEventArgs(true, ex);
                    ChannelCreationFailed?.RaiseEvent(this, eventArgs);
                    await eventArgs.WaitForDeferralsAsync().ConfigureAwait(false);

                    if (!eventArgs.IsHandled) throw;
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return clientChannel;
        }

        private async Task DiscardChannelAsync(IClientChannel clientChannel, CancellationToken cancellationToken)
        {
            clientChannel.DisposeIfDisposable();

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (ReferenceEquals(clientChannel, _clientChannel))
                {
                    _clientChannel = null;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _clientChannel?.DisposeIfDisposable();
            _disposed = true;
        }
    }
}
