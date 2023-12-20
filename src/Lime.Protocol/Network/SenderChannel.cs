using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol.Server;
using Lime.Protocol.Util;

namespace Lime.Protocol.Network
{
    internal sealed class SenderChannel : ISenderChannel, IStoppable, IDisposable
    {
        private readonly IChannelInformation _channelInformation;
        private readonly ITransport _transport;
        private readonly ICollection<IChannelModule<Message>> _messageModules;
        private readonly ICollection<IChannelModule<Notification>> _notificationModules;
        private readonly ICollection<IChannelModule<Command>> _commandModules;
        private readonly Func<Exception, Task> _exceptionHandler;
        private readonly TimeSpan _sendTimeout;
        private readonly TimeSpan _closeTimeout;
        private readonly CancellationTokenSource _senderCts;
        private readonly Channel<Envelope> _envelopeBuffer;
        private readonly SemaphoreSlim _sessionSemaphore;
        private readonly SemaphoreSlim _startStopSemaphore;
        
        private bool _isDisposing;
        private Task _sendToTransportTask;

        public SenderChannel(
            IChannelInformation channelInformation,
            ITransport transport,
            ICollection<IChannelModule<Message>> messageModules,
            ICollection<IChannelModule<Notification>> notificationModules,
            ICollection<IChannelModule<Command>> commandModules,
            Func<Exception, Task> exceptionHandler,
            int envelopeBufferSize,
            TimeSpan sendTimeout,
            TimeSpan? closeTimeout = null)
        {
            if (sendTimeout == default) throw new ArgumentException("Invalid send timeout", nameof(sendTimeout));
            
            _channelInformation = channelInformation;
            _transport = transport;
            _messageModules = messageModules;
            _notificationModules = notificationModules;
            _commandModules = commandModules;
            _exceptionHandler = exceptionHandler;
            _sendTimeout = sendTimeout;
            _closeTimeout = closeTimeout ?? sendTimeout;
            _sessionSemaphore = new SemaphoreSlim(1);
            _startStopSemaphore = new SemaphoreSlim(1);
            _senderCts = new CancellationTokenSource();
            _envelopeBuffer = ChannelUtil.CreateForCapacity<Envelope>(envelopeBufferSize, true, false);
        }

        public async Task SendMessageAsync(Message message, CancellationToken cancellationToken)
            => await SendToBufferAsync(message, _messageModules, cancellationToken);

        public async Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken)
            => await SendToBufferAsync(notification, _notificationModules, cancellationToken);

        public async Task SendCommandAsync(Command command, CancellationToken cancellationToken)
            => await SendToBufferAsync(command, _commandModules, cancellationToken);

        public async Task SendSessionAsync(Session session, CancellationToken cancellationToken)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (_channelInformation.State == SessionState.Finished ||
                _channelInformation.State == SessionState.Failed)
            {
                throw new InvalidOperationException(
                    $"Cannot send a session in the '{_channelInformation.State}' session state");
            }

            await _sessionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Stops the listener task to avoid concurrent writes to the transport
                if (session.State == SessionState.Finishing ||
                    session.State == SessionState.Finished ||
                    session.State == SessionState.Failed)
                {
                    await StopAsync(cancellationToken).ConfigureAwait(false);
                }

                // The session envelopes are sent directly to the transport
                await _transport.SendAsync(session, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _sessionSemaphore.Release();
            }
        }

        public void Start()
        {
            _startStopSemaphore.Wait();
            try
            {
                if (_sendToTransportTask == null)
                {
                    var currentActivity = Activity.Current;
                    Activity.Current = null;

                    // When new tasks are created, the ExecutionContext is captured.
                    // We should not capture the current activity to start a listener, to avoid incorrect activity propagation.
                    _sendToTransportTask = Task.Run(SendToTransportAsync);

                    Activity.Current = currentActivity;
                }
            }
            finally
            {
                _startStopSemaphore.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _startStopSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Complete the buffers and sends after awaiting for the completion
                _envelopeBuffer.Writer.TryComplete();
                
                // Stops the listener task
                _senderCts.CancelIfNotRequested();
                if (_sendToTransportTask != null &&
                    !_sendToTransportTask.IsCompleted)
                {
                    await _sendToTransportTask.WithCancellation(cancellationToken);
                }
            }
            finally
            {
                _startStopSemaphore.Release();
            }
        }

        public void Dispose()
        {
            _isDisposing = true;
            _senderCts.CancelAndDispose();
            _sessionSemaphore.Dispose();
            _startStopSemaphore.Dispose();
        }

        /// <summary>
        /// Sends the envelope to the specified target.
        /// </summary>
        private async Task SendToBufferAsync<T>(T envelope, IEnumerable<IChannelModule<T>> modules, CancellationToken cancellationToken)
            where T : Envelope, new()
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (_channelInformation.State != SessionState.Established)
            {
                throw new InvalidOperationException(
                    $"Cannot send a {typeof(T).Name} in the '{_channelInformation.State}' session state");
            }

            if (!_transport.IsConnected)
            {
                throw new InvalidOperationException("The transport is not connected");
            }

            try
            {
                if (_envelopeBuffer.Reader.Completion.IsCompleted)
                {
                    throw new InvalidOperationException("The send buffer is complete",
                        _envelopeBuffer.Reader.Completion.Exception?.GetBaseException());
                }

                foreach (var module in modules.ToList())
                {
                    if (envelope == null) break;
                    cancellationToken.ThrowIfCancellationRequested();
                    envelope = await module.OnSendingAsync(envelope, cancellationToken).ConfigureAwait(false);
                }

                await _envelopeBuffer.Writer.WriteAsync(envelope, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch when (_transport.IsConnected)
            {
                // Closes the transport in case of any exception
                using var cts = new CancellationTokenSource(_closeTimeout);
                try
                {
                    await _transport.CloseAsync(cts.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    RaiseSenderException(ex);
                }
                throw;
            }
        }
        
        private bool IsChannelEstablished()
            => !_senderCts.IsCancellationRequested &&
               _channelInformation.State == SessionState.Established &&
               _transport.IsConnected;
        
        
        private async Task SendToTransportAsync()
        {
            Exception exception = null;
            
            try
            {
                while (IsChannelEstablished())
                {
                    try
                    {
                        var envelope = await _envelopeBuffer.Reader.ReadAsync(_senderCts.Token).ConfigureAwait(false);
                        if (envelope == null) continue;

                        using var cts = new CancellationTokenSource(_sendTimeout);
                        using var linkedCts =
                            CancellationTokenSource.CreateLinkedTokenSource(_senderCts.Token, cts.Token);

                        await _transport.SendAsync(envelope, linkedCts.Token).ConfigureAwait(false);
                    }
                    catch (EnvelopeTooLargeException ex)
                    {
                        /*This exception is handled since a sender should not close his channel 
                        * if a client requests for a large envelope.*/
                        RaiseSenderException(ex);
                    }
                    catch (ChannelClosedException) 
                    {
                        break;
                    }
                    catch (OperationCanceledException) when (_senderCts.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (ObjectDisposedException) when (_isDisposing)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        RaiseSenderException(ex);
                        break;
                    }
                }
            }
            finally
            {
                _envelopeBuffer.Writer.TryComplete(exception);
                _senderCts.CancelIfNotRequested();
            }
        }

        /// <summary>
        /// Asynchronously raises the channel exception to avoid deadlocks issues.
        /// </summary>
        private void RaiseSenderException(Exception exception)
        {
            Task.Run(() => _exceptionHandler(exception));
        }
    }
}