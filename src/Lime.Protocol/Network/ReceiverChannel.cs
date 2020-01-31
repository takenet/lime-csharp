using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lime.Protocol.Server;
using Lime.Protocol.Util;

namespace Lime.Protocol.Network
{
    internal sealed class ReceiverChannel : IReceiverChannel, IStoppable, IDisposable
    {
        private readonly IChannelInformation _channelInformation;
        private readonly ITransport _transport;
        private readonly IChannelCommandProcessor _channelCommandProcessor;
        private readonly ICollection<IChannelModule<Message>> _messageModules;
        private readonly ICollection<IChannelModule<Notification>> _notificationModules;
        private readonly ICollection<IChannelModule<Command>> _commandModules;
        private readonly Func<Exception, Task> _exceptionHandler;
        private readonly TimeSpan? _consumeTimeout;
        private readonly CancellationTokenSource _consumerCts;
        private readonly Channel<Message> _messageBuffer;
        private readonly Channel<Command> _commandBuffer;
        private readonly Channel<Notification> _notificationBuffer;
        private readonly Channel<Session> _sessionBuffer;
        private readonly SemaphoreSlim _sessionSemaphore;
        private readonly SemaphoreSlim _startStopSemaphore;
        
        private Task _receiveFromTransportTask;
        private bool _isDisposing;

        public ReceiverChannel(
            IChannelInformation channelInformation,
            ITransport transport,
            IChannelCommandProcessor channelCommandProcessor,
            ICollection<IChannelModule<Message>> messageModules,
            ICollection<IChannelModule<Notification>> notificationModules,
            ICollection<IChannelModule<Command>> commandModules,
            Func<Exception, Task> exceptionHandler,
            int envelopeBufferSize,
            TimeSpan? consumeTimeout)
        {
            if (consumeTimeout != null && consumeTimeout.Value == default) throw new ArgumentException("Invalid consume timeout", nameof(consumeTimeout));
            
            _channelInformation = channelInformation;
            _transport = transport;
            _channelCommandProcessor = channelCommandProcessor;
            _messageModules = messageModules;
            _notificationModules = notificationModules;
            _commandModules = commandModules;
            _exceptionHandler = exceptionHandler;
            _consumeTimeout = consumeTimeout;
            _sessionSemaphore = new SemaphoreSlim(1);
            _startStopSemaphore = new SemaphoreSlim(1);
            _consumerCts = new CancellationTokenSource();
            _messageBuffer = ChannelUtil.CreateForCapacity<Message>(envelopeBufferSize, false, true);
            _notificationBuffer = ChannelUtil.CreateForCapacity<Notification>(envelopeBufferSize, false, true);
            _commandBuffer = ChannelUtil.CreateForCapacity<Command>(envelopeBufferSize, false, true);
            _sessionBuffer = ChannelUtil.CreateForCapacity<Session>(envelopeBufferSize, false, true);
        }
        
        /// <inheritdoc />
        public Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
            => ReceiveFromBufferAsync(_messageBuffer.Reader, _messageModules, cancellationToken);
        
        /// <inheritdoc />
        public Task<Command> ReceiveCommandAsync(CancellationToken cancellationToken)
            => ReceiveFromBufferAsync(_commandBuffer.Reader, _commandModules, cancellationToken);
        
        /// <inheritdoc />
        public Task<Notification> ReceiveNotificationAsync(CancellationToken cancellationToken)
            => ReceiveFromBufferAsync(_notificationBuffer.Reader, _notificationModules, cancellationToken);
        
        /// <inheritdoc />
        public async Task<Session> ReceiveSessionAsync(CancellationToken cancellationToken)
        {
            switch (_channelInformation.State)
            {
                case SessionState.Finished:
                    throw new InvalidOperationException(
                        $"Cannot receive a session in the '{_channelInformation.State}' session state");

                case SessionState.Established:
                    return await ReceiveFromBufferAsync(_sessionBuffer.Reader, Enumerable.Empty<IChannelModule<Session>>(), cancellationToken)
                        .ConfigureAwait(false);
            }

            await _sessionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // The session envelopes are received directly from the transport, except when the session is established
                var envelope = await _transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                if (envelope is Session session) return session;

                throw new InvalidOperationException("An empty or unexpected envelope was received from the transport");
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
                if (_receiveFromTransportTask == null)
                {
                    _receiveFromTransportTask = Task.Run(ReceiveFromTransportAsync);
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
                // Complete the pipeline
                TryCompleteWriters();

                // Stops the listener task
                _consumerCts.CancelIfNotRequested();
                if (_receiveFromTransportTask != null &&
                    !_receiveFromTransportTask.IsCompleted)
                {
                    await _receiveFromTransportTask.WithCancellation(cancellationToken);
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
            _consumerCts.CancelIfNotRequested();
            _consumerCts.Dispose();
            _sessionSemaphore.Dispose();
            _startStopSemaphore.Dispose();
        }

        private bool IsChannelEstablished()
            => !_consumerCts.IsCancellationRequested &&
                _channelInformation.State == SessionState.Established &&
                _transport.IsConnected;

        private async Task ReceiveFromTransportAsync()
        {
            Exception exception = null;

            try
            {
                while (IsChannelEstablished())
                {
                    try
                    {
                        var envelope = await _transport.ReceiveAsync(_consumerCts.Token).ConfigureAwait(false);
                        if (envelope == null) continue;

                        using var timeoutCts = _consumeTimeout == null 
                            ? new CancellationTokenSource() 
                            : new CancellationTokenSource(_consumeTimeout.Value);
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, _consumerCts.Token);
                        
                        try
                        {
                            await WriteToBufferAsync(envelope, linkedCts.Token).ConfigureAwait(false);
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
                        exception = ex;
                        RaiseReceiverException(ex);
                        break;
                    }
                }
            }
            finally
            {
                TryCompleteWriters(exception);
                _channelCommandProcessor.CancelAll();
                _consumerCts.CancelIfNotRequested();
            }
        }

        private async Task WriteToBufferAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            switch (envelope)
            {
                case Message message:
                    await _messageBuffer.Writer.WriteAsync(message, cancellationToken);
                    break;

                case Notification notification:
                    await _notificationBuffer.Writer.WriteAsync(notification, cancellationToken);
                    break;
                
                case Command command:
                    if (!_channelCommandProcessor.TrySubmitCommandResult(command))
                    {
                        await _commandBuffer.Writer.WriteAsync(command, cancellationToken);
                    }
                    break;
                
                case Session session:
                    await _sessionBuffer.Writer.WriteAsync(session, cancellationToken);
                    break;
                
                default:
                    throw new ArgumentException($"Unknown envelope type {envelope.GetType()}");
            }
        }

        /// <summary>
        /// Receives an envelope from the buffer.
        /// </summary>
        private async Task<T> ReceiveFromBufferAsync<T>(ChannelReader<T> reader, IEnumerable<IChannelModule<T>> modules, CancellationToken cancellationToken) 
            where T : Envelope, new()
        {
            if (_channelInformation.State < SessionState.Established)
            {
                throw new InvalidOperationException($"Cannot receive envelopes in the '{_channelInformation.State}' session state");
            }
            
            try
            {
                T envelope;
                var modulesList = modules.ToList();
                
                do
                {
                    envelope = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                    foreach (var module in modulesList)
                    {
                        if (envelope == null) break;
                        envelope = await module.OnReceivingAsync(envelope, cancellationToken);
                    }
                } while (envelope == null);

                return envelope;
            }
            catch (ChannelClosedException ex) when (reader.Completion.IsCompleted)
            {
                throw new InvalidOperationException("The channel listener task is complete and cannot receive envelopes", ex);
            }
        }
        
        private void TryCompleteWriters(Exception exception = null)
        {
            _messageBuffer.Writer.TryComplete(exception);
            _notificationBuffer.Writer.TryComplete(exception);
            _commandBuffer.Writer.TryComplete(exception);
            _sessionBuffer.Writer.TryComplete(exception);
        }
        
        /// <summary>
        /// Asynchronously raises the channel exception to avoid deadlocks issues.
        /// </summary>
        private void RaiseReceiverException(Exception exception)
        {
            Task.Run(() => _exceptionHandler(exception));
        }
    }
}