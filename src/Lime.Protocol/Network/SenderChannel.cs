using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Server;

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
            int sendBatchSize,
            TimeSpan flushBatchInterval)
        {
            if (sendTimeout == default) throw new ArgumentException("Invalid send timeout", nameof(sendTimeout));
            if (sendBatchSize <= 0) throw new ArgumentOutOfRangeException(nameof(sendBatchSize));
            
            _channelInformation = channelInformation;
            _transport = transport;
            _messageModules = messageModules;
            _notificationModules = notificationModules;
            _commandModules = commandModules;
            _exceptionHandler = exceptionHandler;
            _sendTimeout = sendTimeout;
            _sessionSemaphore = new SemaphoreSlim(1);
            _startStopSemaphore = new SemaphoreSlim(1);

            // Send pipeline
            // Modules blocks
            _senderCts = new CancellationTokenSource();

            _envelopeBuffer = envelopeBufferSize > 0
                ? Channel.CreateBounded<Envelope>(new BoundedChannelOptions(envelopeBufferSize)
                {
                    SingleReader = true,
                    SingleWriter = false
                })
                : Channel.CreateUnbounded<Envelope>(new UnboundedChannelOptions()
                {
                    SingleReader = true,
                    SingleWriter = false
                });
        }

        public Task SendMessageAsync(Message message, CancellationToken cancellationToken)
            => SendAsync(message, _messageModules, cancellationToken);

        public Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken)
            => SendAsync(notification, _notificationModules, cancellationToken);

        public Task SendCommandAsync(Command command, CancellationToken cancellationToken)
            => SendAsync(command, _commandModules, cancellationToken);

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
                    _sendToTransportTask = Task.Run(SendToTransportAsync);
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
                    //await _sendToTransportTask.WithCancellation(cancellationToken);
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
            _senderCts.CancelIfNotRequested();
            _senderCts.Dispose();
            _sessionSemaphore.Dispose();
            _startStopSemaphore.Dispose();
        }

        /// <summary>
        /// Sends the envelope to the specified target.
        /// </summary>
        private async Task SendAsync<T>(T envelope, IEnumerable<IChannelModule<T>> modules, CancellationToken cancellationToken)
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
                // Should the consumer complete the writer?
                _senderCts.CancelIfNotRequested();
                _envelopeBuffer.Writer.Complete(exception);
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