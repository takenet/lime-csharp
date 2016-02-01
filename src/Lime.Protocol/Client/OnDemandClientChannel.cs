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
        private readonly EstablishedClientChannelBuilder _builder;
        private readonly TimeSpan _sendTimeout;
        private readonly SemaphoreSlim _semaphore;
        private IClientChannel _clientChannel;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnDemandClientChannel"/> class.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public OnDemandClientChannel(EstablishedClientChannelBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            _builder = builder;
            _sendTimeout = builder.ChannelBuilder.SendTimeout;
            _semaphore = new SemaphoreSlim(0, 1);
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
        /// Occurs when the channel creation failed.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ChannelCreationFailed;

        /// <summary>
        /// Occurs when the channel send or receive action failed.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ChannelOperationFailed;

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

        private async Task<IClientChannel> GetChannelAsync(CancellationToken cancellationToken)
        {
            while (_clientChannel == null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (_clientChannel == null)
                    {
                        _clientChannel = await _builder
                            .BuildAndEstablishAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    var eventArgs = new ExceptionEventArgs(ex);                                                                
                    ChannelCreationFailed?.RaiseEvent(this, eventArgs);
                    await eventArgs.WaitForDeferralsAsync().ConfigureAwait(false);
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return _clientChannel;
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
                    break;
                }
                catch (InvalidOperationException)
                {
                    channel.DisposeIfDisposable();
                    _clientChannel = null;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    var eventArgs = new ExceptionEventArgs(ex);
                    ChannelOperationFailed?.RaiseEvent(this, eventArgs);
                    await eventArgs.WaitForDeferralsAsync().ConfigureAwait(false);
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
                catch (InvalidOperationException)
                {
                    channel.DisposeIfDisposable();
                    _clientChannel = null;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    var eventArgs = new ExceptionEventArgs(ex);
                    ChannelOperationFailed?.RaiseEvent(this, eventArgs);
                    await eventArgs.WaitForDeferralsAsync().ConfigureAwait(false);
                }
            }

            throw new ObjectDisposedException(nameof(OnDemandClientChannel));
        }

        public void Dispose()
        {
            _clientChannel?.DisposeIfDisposable();
            _disposed = true;
        }
    }
}
