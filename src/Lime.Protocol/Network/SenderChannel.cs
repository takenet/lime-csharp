using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Server;

namespace Lime.Protocol.Network
{
    internal sealed class SenderChannel : ISenderChannel, IFlushable, IStoppable, IDisposable
    {
        private readonly IChannelInformation _channelInformation;
        private readonly ITransport _transport;
        private readonly ICollection<IChannelModule<Message>> _messageModules;
        private readonly ICollection<IChannelModule<Notification>> _notificationModules;
        private readonly ICollection<IChannelModule<Command>> _commandModules;
        private readonly Func<Exception, Task> _exceptionHandler;
        
        private readonly TimeSpan _sendTimeout;
        private readonly CancellationTokenSource _senderCts;
        private readonly ActionBlock<(Envelope, TaskCompletionSource<Envelope>)> _sendEnvelopeBlock;
        private readonly object _syncRoot;
        private bool _isDisposing;


        public SenderChannel(
            IChannelInformation channelInformation,
            ITransport transport,
            ICollection<IChannelModule<Message>> messageModules,
            ICollection<IChannelModule<Notification>> notificationModules,
            ICollection<IChannelModule<Command>> commandModules,
            Func<Exception, Task> exceptionHandler,
            int envelopeBufferSize,
            TimeSpan sendTimeout)
        {
            _channelInformation = channelInformation;
            _transport = transport;
            _messageModules = messageModules;
            _notificationModules = notificationModules;
            _commandModules = commandModules;
            _exceptionHandler = exceptionHandler;
            _sendTimeout = sendTimeout;

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
            _syncRoot = new object();
        }
        

        /// <inheritdoc />
        public Task SendMessageAsync(Message message, CancellationToken cancellationToken)
            => SendAsync(message, cancellationToken, _messageModules);

        public Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken)
            => SendAsync(notification, cancellationToken, _notificationModules);

        public Task SendCommandAsync(Command command, CancellationToken cancellationToken)
            => SendAsync(command, cancellationToken, _commandModules);

        public async Task SendSessionAsync(Session session, CancellationToken cancellationToken)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (_channelInformation.State == SessionState.Finished || _channelInformation.State == SessionState.Failed)
            {
                throw new InvalidOperationException($"Cannot send a session in the '{_channelInformation.State}' session state");
            }

            await SendToBufferAsync(session, cancellationToken);
            await FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        
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
        
        public Task StopAsync(CancellationToken cancellationToken)
        {
            lock (_syncRoot)
            {
                _senderCts.CancelIfNotRequested();
            }
            return Task.CompletedTask;
        }
        
        
        public void Dispose()
        {
            _isDisposing = true;
            _senderCts.CancelIfNotRequested();
            _senderCts.Dispose();
        }
        
        /// <summary>
        /// Sends the envelope to the transport.
        /// </summary>
        private async Task SendAsync<T>(T envelope, CancellationToken cancellationToken, IEnumerable<IChannelModule<T>> modules) 
            where T : Envelope, new()
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (_channelInformation.State != SessionState.Established)
            {
                throw new InvalidOperationException($"Cannot send a {typeof(T).Name} in the '{_channelInformation.State}' session state");
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
            if (!_transport.IsConnected)
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
                    await _transport.SendAsync(envelope, linkedCts.Token).ConfigureAwait(false);
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
                
                _sendEnvelopeBlock.Complete();
                _senderCts.CancelIfNotRequested();
                sentTcs?.TrySetException(ex);
                
                await RaiseSenderExceptionAsync(ex);
            }
        }
        
        private Task RaiseSenderExceptionAsync(Exception exception) => _exceptionHandler(exception);
        
    }

    public static class CancellationTokenSourceExtensions
    {
        public static void CancelIfNotRequested(this CancellationTokenSource cts)
        {
            if (!cts.IsCancellationRequested) cts.Cancel();
        }
    }
}