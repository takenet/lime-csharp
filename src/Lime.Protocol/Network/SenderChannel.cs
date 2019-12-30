using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private readonly TimeSpan _sendTimeout;
        private readonly CancellationTokenSource _senderCts;

        private readonly TransformBlock<Message, Envelope> _messageToEnvelopeTransformBlock;
        private readonly TransformBlock<Notification, Envelope> _notificationToEnvelopeTransformBlock;
        private readonly TransformBlock<Command, Envelope> _commandToEnvelopeTransformBlock;
        private readonly BatchBlock<Envelope> _envelopeBatchBlock;
        private readonly ActionBlock<Envelope[]> _sendEnvelopeBatchBlock;
        private readonly ActionBlock<Envelope> _sendEnvelopeBlock;
        private readonly SemaphoreSlim _sessionSemaphore;
        private readonly SemaphoreSlim _startStopSemaphore;
        private readonly ActionBlock<Exception> _exceptionHandlerActionBlock;

        private readonly ITargetBlock<Envelope> _skipModulesTargetBlock; // Optimization: Reference to the target block to send directly skipping the modules

        private bool _isDisposing;
        private readonly Timer _flushBatchTimer;
        private readonly TimeSpan _flushBatchInterval;
        
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
            _sendTimeout = sendTimeout;
            _exceptionHandlerActionBlock = new ActionBlock<Exception>(exceptionHandler, DataflowUtils.UnboundedUnorderedExecutionDataflowBlockOptions);
            _sessionSemaphore = new SemaphoreSlim(1);
            _startStopSemaphore = new SemaphoreSlim(1);

            // Send pipeline
            // Modules blocks
            _senderCts = new CancellationTokenSource();
            var raiseModulesDataflowBlockOptions = new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = envelopeBufferSize,
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                EnsureOrdered = false
            };
            _messageToEnvelopeTransformBlock = new TransformBlock<Message, Envelope>(
                e => RaiseOnSendingAsync(e, _messageModules, _senderCts.Token), raiseModulesDataflowBlockOptions);
            _notificationToEnvelopeTransformBlock = new TransformBlock<Notification, Envelope>(
                e => RaiseOnSendingAsync(e, _notificationModules, _senderCts.Token), raiseModulesDataflowBlockOptions);
            _commandToEnvelopeTransformBlock = new TransformBlock<Command, Envelope>(
                e => RaiseOnSendingAsync(e, _commandModules, _senderCts.Token), raiseModulesDataflowBlockOptions);
            var sendDataflowBlockOptions = new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = envelopeBufferSize,
                MaxDegreeOfParallelism = 1,
                EnsureOrdered = false,
            };

            // Create the after modules pipeline, depending if it is batched or not.
            if (sendBatchSize <= 1)
            {
                // Single mode, send directly to the the send block
                _skipModulesTargetBlock = _sendEnvelopeBlock = new ActionBlock<Envelope>(
                    e => SendToTransportAsync(e, _senderCts.Token),
                    sendDataflowBlockOptions);
                _messageToEnvelopeTransformBlock.LinkTo(_sendEnvelopeBlock,
                    DataflowUtils.PropagateCompletionLinkOptions);
                _notificationToEnvelopeTransformBlock.LinkTo(_sendEnvelopeBlock,
                    DataflowUtils.PropagateCompletionLinkOptions);
                _commandToEnvelopeTransformBlock.LinkTo(_sendEnvelopeBlock,
                    DataflowUtils.PropagateCompletionLinkOptions);
            }
            else
            {
                // Batched mode, create a batch block between the modules block and the send block 
                _skipModulesTargetBlock = _envelopeBatchBlock = new BatchBlock<Envelope>(
                    sendBatchSize,
                    new GroupingDataflowBlockOptions()
                    {
                        BoundedCapacity = envelopeBufferSize,
                        MaxNumberOfGroups = DataflowBlockOptions.Unbounded,
                        EnsureOrdered = false,
                    });
                _sendEnvelopeBatchBlock = new ActionBlock<Envelope[]>(
                    e => SendToTransportAsync(e, _senderCts.Token),
                    sendDataflowBlockOptions);

                _messageToEnvelopeTransformBlock.LinkTo(_envelopeBatchBlock,
                    DataflowUtils.PropagateCompletionLinkOptions);
                _notificationToEnvelopeTransformBlock.LinkTo(_envelopeBatchBlock,
                    DataflowUtils.PropagateCompletionLinkOptions);
                _commandToEnvelopeTransformBlock.LinkTo(_envelopeBatchBlock,
                    DataflowUtils.PropagateCompletionLinkOptions);
                _envelopeBatchBlock.LinkTo(_sendEnvelopeBatchBlock, DataflowUtils.PropagateCompletionLinkOptions);
                _flushBatchInterval = flushBatchInterval;

                if (flushBatchInterval != default)
                {
                    _flushBatchTimer = new Timer(state =>
                        {
                            if (!_envelopeBatchBlock.Completion.IsCompleted)
                            {
                                _envelopeBatchBlock.TriggerBatch();
                            }
                        },
                        null,
                        _flushBatchInterval,
                        _flushBatchInterval);
                }
            }
        }

        public Task SendMessageAsync(Message message, CancellationToken cancellationToken)
            =>  _messageModules.Count > 0 
                ? SendAsync(message, _messageToEnvelopeTransformBlock, cancellationToken) 
                : SendAsync(message, _skipModulesTargetBlock, cancellationToken); // Skip modules if there's none to optimize the pipeline

        public Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken)
            =>  _notificationModules.Count > 0 
                ? SendAsync(notification, _notificationToEnvelopeTransformBlock, cancellationToken)
                : SendAsync(notification, _skipModulesTargetBlock, cancellationToken);

        public Task SendCommandAsync(Command command, CancellationToken cancellationToken)
            =>  _commandModules.Count > 0 
                ? SendAsync(command, _commandToEnvelopeTransformBlock, cancellationToken)
                : SendAsync(command, _skipModulesTargetBlock, cancellationToken);

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
                await SendToTransportAsync(session, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _sessionSemaphore.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _startStopSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Complete the buffers and sends after awaiting for the completion
                CompletePipeline();

                // Awaits the completion of the last pipeline block (depending if it is batched or not)
                await (_sendEnvelopeBlock?.Completion ?? _sendEnvelopeBatchBlock.Completion).WithCancellation(cancellationToken).ConfigureAwait(false);                
                
                _senderCts.CancelIfNotRequested();
                _flushBatchTimer?.Change(Timeout.Infinite, Timeout.Infinite);
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
            _flushBatchTimer?.Dispose();
        }

        /// <summary>
        /// Sends the envelope to the specified target.
        /// </summary>
        private async Task SendAsync<T>(T envelope, ITargetBlock<T> targetBlock, CancellationToken cancellationToken)
            where T : Envelope
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

            if (targetBlock.Completion.IsCompleted)
            {
                throw new InvalidOperationException("The send buffer is complete",
                    targetBlock.Completion.Exception?.GetBaseException());
            }
            if (!await targetBlock.SendAsync(envelope, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The send buffer is not accepting more envelopes");
            }
        }

        private async Task<Envelope> RaiseOnSendingAsync<T>(T envelope, IEnumerable<IChannelModule<T>> modules, CancellationToken cancellationToken)
            where T : Envelope, new()
        {
            try
            {
                foreach (var module in modules.ToList())
                {
                    if (envelope == null) break;
                    cancellationToken.ThrowIfCancellationRequested();
                    envelope = await module.OnSendingAsync(envelope, cancellationToken).ConfigureAwait(false);
                }

                return envelope;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                await RaiseSenderExceptionAsync(ex);                
            }

            return null;
        }

        private async Task SendToTransportAsync(Envelope[] envelopes, CancellationToken cancellationToken)
        {
            try
            {
                _flushBatchTimer?.Change(_flushBatchInterval, _flushBatchInterval);

                foreach (var envelope in envelopes)
                {
                    if (!await SendToTransportAsync(envelope, cancellationToken).ConfigureAwait(false))
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                await RaiseSenderExceptionAsync(ex);
            }
        }

        private async Task<bool> SendToTransportAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            try
            {
                if (envelope == null) return true;

                using var cts = new CancellationTokenSource(_sendTimeout);
                using var linkedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                await _transport.SendAsync(envelope, linkedCts.Token).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (_isDisposing)
            {
            }
            catch (Exception ex)
            {
                await RaiseSenderExceptionAsync(ex);
            }

            return false;
        }

        private void CompletePipeline()
        {
            _messageToEnvelopeTransformBlock.CompleteIfNotCompleted();
            _notificationToEnvelopeTransformBlock.CompleteIfNotCompleted();
            _commandToEnvelopeTransformBlock.CompleteIfNotCompleted();
        }

        /// <summary>
        /// Asynchronously raises the channel exception to avoid deadlocks issues.
        /// </summary>
        private Task RaiseSenderExceptionAsync(Exception exception) => _exceptionHandlerActionBlock.SendAsync(exception);
    }
}