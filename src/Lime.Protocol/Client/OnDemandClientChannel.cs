using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private readonly SemaphoreSlim _semaphore;
        private IClientChannel _clientChannel;
        private bool _disposed;

        private Task<Session> _finishedSessionTask;
        private CancellationTokenSource _cts;

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
            _semaphore = new SemaphoreSlim(1, 1);

            ChannelCreatedHandlers = new List<Func<ChannelInformation, Task>>();
            ChannelDiscardedHandlers = new List<Func<ChannelInformation, Task>>();
            ChannelCreationFailedHandlers = new List<Func<FailedChannelInformation, Task<bool>>>();
            ChannelOperationFailedHandlers = new List<Func<FailedChannelInformation, Task<bool>>>();
        }

        /// <summary>
        /// Sends a command envelope to the remote node.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task SendCommandAsync(Command command, CancellationToken cancellationToken)
        {
            return SendAsync(command, cancellationToken, (channel, envelope) => channel.SendCommandAsync(envelope, cancellationToken));
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
        /// Processes the command asynchronous.
        /// </summary>
        /// <param name="requestCommand">The request command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ObjectDisposedException"></exception>
        public async Task<Command> ProcessCommandAsync(Command requestCommand, CancellationToken cancellationToken)
        {
            while (!_disposed)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var channel = await GetChannelAsync(cancellationToken, nameof(ProcessCommandAsync)).ConfigureAwait(false);
                try
                {
                    return await channel.ProcessCommandAsync(requestCommand, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (!await HandleChannelOperationExceptionAsync(ex, nameof(ProcessCommandAsync), channel, cancellationToken)) throw;
                }
            }

            throw new ObjectDisposedException(nameof(OnDemandClientChannel));
        }

        /// <summary>
        /// Sends a message to the remote node.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task SendMessageAsync(Message message, CancellationToken cancellationToken)
        {
            return SendAsync(message, cancellationToken, (channel, envelope) => channel.SendMessageAsync(envelope, cancellationToken));
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
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken)
        {
            return SendAsync(notification, cancellationToken, (channel, envelope) => channel.SendNotificationAsync(envelope, cancellationToken));
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
        public bool IsEstablished => ChannelIsEstablished(_clientChannel);

        /// <summary>
        /// Force the creation of an established client channel, if not established.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public Task EstablishAsync(CancellationToken cancellationToken) => 
            GetChannelAsync(cancellationToken, nameof(EstablishAsync));

        /// <summary>
        /// Gets the channel created handlers, which are called when a channel is created.
        /// </summary>
        public ICollection<Func<ChannelInformation, Task>> ChannelCreatedHandlers { get; }

        /// <summary>
        /// Gets the channel discarded handlers, which are called when a channel is discarded.
        /// </summary>
        public ICollection<Func<ChannelInformation, Task>> ChannelDiscardedHandlers { get; }

        /// <summary>
        /// Gets the channel creation failed handlers, which are called when the channel creation failed.
        /// Each handler must return <c>true</c> if the failure was handled and a channel should be created again or <c>false</c> if not, which causes the exception to be thrown to the caller.
        /// The default action is the recreation of a channel. If a single handler return <c>false</c>, no channel will not be recreated.
        /// </summary>
        public ICollection<Func<FailedChannelInformation, Task<bool>>> ChannelCreationFailedHandlers { get; }

        /// <summary>
        /// Gets the channel operation failed handlers, which are called when the channel fails during an operation.
        /// Each handler must return <c>true</c> if the failure was handled and a channel should be created again or <c>false</c> if not, which causes the exception to be thrown to the caller.
        /// The default action is the recreation of a channel. If a single handler return <c>false</c>, no channel will not be recreated.
        /// </summary>
        public ICollection<Func<FailedChannelInformation, Task<bool>>> ChannelOperationFailedHandlers { get; }

        /// <summary>
        /// Finishes the associated client channel, if established.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task FinishAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (IsEstablished &&
                    _finishedSessionTask != null)
                {
                    await _clientChannel.SendFinishingSessionAsync(cancellationToken).ConfigureAwait(false);
                    await _finishedSessionTask.ConfigureAwait(false);
                }

                if (_clientChannel != null)
                {
                    await DiscardChannelUnsynchronizedAsync(_clientChannel, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task SendAsync<T>(T envelope, CancellationToken cancellationToken, Func<IClientChannel, T, Task> sendFunc, [CallerMemberName]string operationName = null) where T : Envelope, new()
        {
            while (!_disposed)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var channel = await GetChannelAsync(cancellationToken, operationName).ConfigureAwait(false);
                try
                {
                    await sendFunc(channel, envelope).ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (!await HandleChannelOperationExceptionAsync(ex, operationName, channel, cancellationToken)) throw;
                }
            }

            throw new ObjectDisposedException(nameof(OnDemandClientChannel));
        }

        private async Task<T> ReceiveAsync<T>(CancellationToken cancellationToken, Func<IClientChannel, CancellationToken, Task<T>> receiveFunc, [CallerMemberName]string operationName = null) where T : Envelope, new()
        {
            while (!_disposed)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // For receiving, we should not check if the channel is established, since that can exists received envelopes in the buffer.
                var channel = await GetChannelAsync(cancellationToken, operationName).ConfigureAwait(false);
                try
                {
                    return await receiveFunc(channel, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (!await HandleChannelOperationExceptionAsync(ex, operationName, channel, cancellationToken)) throw;
                }
            }

            throw new ObjectDisposedException(nameof(OnDemandClientChannel));
        }

        private async Task<bool> HandleChannelOperationExceptionAsync(Exception ex, string operationName, IChannel channel, CancellationToken cancellationToken)
        {
            try
            {
                var failedChannelInformation = new FailedChannelInformation(
                    channel.SessionId, channel.State, channel.LocalNode, channel.RemoteNode, channel.Transport.IsConnected, ex, operationName);

                // Make a copy of the handlers
                var handlers = ChannelOperationFailedHandlers.ToList();
                return await InvokeHandlersAsync(handlers, failedChannelInformation, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await DiscardChannelAsync(channel, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<bool> InvokeHandlersAsync(IEnumerable<Func<FailedChannelInformation, Task<bool>>> handlers, FailedChannelInformation failedChannelInformation, CancellationToken cancellationToken)
        {
            var exceptions = new List<Exception>();
            var handled = true;
            foreach (var handler in handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (!await handler(failedChannelInformation).ConfigureAwait(false))
                    {
                        handled = false;
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            ThrowIfAny(exceptions);
            return handled;
        }

        private static async Task InvokeHandlersAsync(IEnumerable<Func<ChannelInformation, Task>> handlers, ChannelInformation channelInformation, CancellationToken cancellationToken)
        {
            var exceptions = new List<Exception>();
            foreach (var handler in handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await handler(channelInformation).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            ThrowIfAny(exceptions);
        }

        private static void ThrowIfAny(IReadOnlyList<Exception> exceptions)
        {
            if (!exceptions.Any()) return;
            if (exceptions.Count == 1) throw exceptions[0];
            throw new AggregateException(exceptions);
        }

        private async Task<IClientChannel> GetChannelAsync(CancellationToken cancellationToken, string operationName)
        {
            var channelCreated = false;
            var clientChannel = _clientChannel;
            while (ShouldCreateChannel(clientChannel))
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    clientChannel = _clientChannel;
                    if (ShouldCreateChannel(clientChannel))
                    {
                        try
                        {
                            _cts?.Cancel();
                            _cts?.Dispose();
                            _clientChannel?.DisposeIfDisposable();
                        }
                        catch { }
                        finally
                        {
                            _cts = null;
                        }

                        clientChannel = _clientChannel = await _builder
                            .BuildAndEstablishAsync(cancellationToken)
                            .ConfigureAwait(false);

                        _cts = new CancellationTokenSource();
                        _finishedSessionTask = clientChannel.ReceiveFinishedSessionAsync(_cts.Token);
                        channelCreated = true;
                        break;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var failedChannelInformation = new FailedChannelInformation(
                        null, SessionState.New, null, null, false, ex, operationName);

                    var handlers = ChannelCreationFailedHandlers.ToList();
                    if (!await InvokeHandlersAsync(handlers, failedChannelInformation, cancellationToken).ConfigureAwait(false)) throw;
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            if (channelCreated && clientChannel != null)
            {
                var channelInformation = new ChannelInformation(clientChannel.SessionId, clientChannel.State, clientChannel.LocalNode, clientChannel.RemoteNode);
                var handlers = ChannelCreatedHandlers.ToList();
                await InvokeHandlersAsync(handlers, channelInformation, cancellationToken).ConfigureAwait(false);
            }

            return clientChannel;
        }

        private static bool ShouldCreateChannel(IChannel channel)
        {
            return channel == null || !ChannelIsEstablished(channel);
        }

        private static bool ChannelIsEstablished(IChannel channel) => channel != null &&
                                                                      channel.State == SessionState.Established &&
                                                                      channel.Transport.IsConnected;

        private async Task DiscardChannelAsync(IChannel clientChannel, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await DiscardChannelUnsynchronizedAsync(clientChannel, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task DiscardChannelUnsynchronizedAsync(IChannel clientChannel, CancellationToken cancellationToken)
        {            
            if (ReferenceEquals(clientChannel, _clientChannel))
            {
                _clientChannel = null;
                clientChannel.DisposeIfDisposable();
                var channelInformation = new ChannelInformation(clientChannel.SessionId, clientChannel.State, clientChannel.LocalNode, clientChannel.RemoteNode);
                var handlers = ChannelDiscardedHandlers.ToList();
                await InvokeHandlersAsync(handlers, channelInformation, cancellationToken).ConfigureAwait(false);                
            }
        }

        public void Dispose()
        {
            _clientChannel?.DisposeIfDisposable();
            _clientChannel = null;
            _semaphore?.DisposeIfDisposable();
            _cts?.Dispose();
            _disposed = true;
        }
    }
}
