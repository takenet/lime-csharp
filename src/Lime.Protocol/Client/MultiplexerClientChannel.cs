using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;

namespace Lime.Protocol.Client
{
    /// <summary>
    /// Implements a client channel that hold multiple connections with the server and distribute the load between these channels.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Client.IOnDemandClientChannel" />
    /// <seealso cref="System.IDisposable" />
    public sealed class MultiplexerClientChannel : IOnDemandClientChannel, IDisposable
    {
        private readonly IOnDemandClientChannel[] _channels;
        private readonly IChannelListener[] _listeners;
        private readonly SemaphoreSlim _semaphore;
        private readonly BufferBlock<BufferedEnvelope> _outputBufferBlock;
        private readonly ActionBlock<BufferedEnvelope>[] _outputActionBlocks;
        private readonly BufferBlock<Message> _inputMessageBufferBlock;
        private readonly BufferBlock<Notification> _inputNotificationBufferBlock;
        private readonly TransformBlock<Command, Command> _processCommandTransformBlock;
        private readonly BufferBlock<Command> _inputCommandBufferBlock;
        private readonly IChannelCommandProcessor _channelCommandProcessor;

        private long _outputCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiplexerClientChannel"/> class.
        /// </summary>
        /// <param name="builder">The channel builder.</param>
        /// <param name="count">The number of channels to create.</param>
        /// <param name="inputBufferSize">The input buffer bounded capacity.</param>
        /// <param name="outputBufferSize">The output buffer bounded capacity.</param>
        /// <param name="channelCommandProcessor">The workflow for processing commands.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public MultiplexerClientChannel(
            IEstablishedClientChannelBuilder builder, 
            int count = 5,
            int inputBufferSize = 1,
            int outputBufferSize = 1,
            IChannelCommandProcessor channelCommandProcessor = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

            // Create observable collections to allow synchronization
            var channelCreatedHandlers = new ObservableCollection<Func<ChannelInformation, Task>>();
            var channelDiscardedHandlers = new ObservableCollection<Func<ChannelInformation, Task>>();
            var channelCreationFailedHandlers = new ObservableCollection<Func<FailedChannelInformation, Task<bool>>>();
            var channelOperationFailedHandlers = new ObservableCollection<Func<FailedChannelInformation, Task<bool>>>();
            ChannelCreatedHandlers = channelCreatedHandlers;
            ChannelDiscardedHandlers = channelDiscardedHandlers;
            ChannelCreationFailedHandlers = channelCreationFailedHandlers;
            ChannelOperationFailedHandlers = channelOperationFailedHandlers;

            // Global input buffers
            var inputOptions = new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = inputBufferSize,
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                EnsureOrdered = false
            };
            _inputMessageBufferBlock = new BufferBlock<Message>(inputOptions);
            _inputNotificationBufferBlock = new BufferBlock<Notification>(inputOptions);
            _channelCommandProcessor = channelCommandProcessor ?? new ChannelCommandProcessor();
            // Uses the same channel command processor for all instances
            // to avoid problems with commands responses being received on different channels.
            builder.ChannelBuilder.WithChannelCommandProcessor(_channelCommandProcessor);
            _processCommandTransformBlock = new TransformBlock<Command, Command>(c =>
            {
                if (_channelCommandProcessor.TrySubmitCommandResult(c)) return null;
                return c;
            },
            inputOptions);
            _inputCommandBufferBlock = new BufferBlock<Command>(inputOptions);
            _processCommandTransformBlock.LinkTo(_inputCommandBufferBlock, c => c != null);
            _processCommandTransformBlock.LinkTo(DataflowBlock.NullTarget<Command>(), c => c == null);
            
            // The global output buffer
            _outputBufferBlock = new BufferBlock<BufferedEnvelope>(new DataflowBlockOptions()
            {
                BoundedCapacity = outputBufferSize,
                EnsureOrdered = false
            });

            // An output action block per channel
            _outputActionBlocks = new ActionBlock<BufferedEnvelope>[count];
            _channels = new IOnDemandClientChannel[count];
            _listeners = new IChannelListener[count];

            for (var i = 0; i < _channels.Length; i++)
            {
                // Add an instance suffix to the builder
                var currentBuilder = builder
                    .Copy()
                    .WithInstance($"{builder.Instance}-{i+1}");
                var channel = new OnDemandClientChannel(currentBuilder);
                
                // Synchronize the handlers
                AttachCollection(channelCreatedHandlers, channel.ChannelCreatedHandlers);
                AttachCollection(channelDiscardedHandlers, channel.ChannelDiscardedHandlers);
                AttachCollection(channelCreationFailedHandlers, channel.ChannelCreationFailedHandlers);
                AttachCollection(channelOperationFailedHandlers, channel.ChannelOperationFailedHandlers);

                // Setup the listener for the channel
                _listeners[i] = new DataflowChannelListener(
                    _inputMessageBufferBlock,
                    _inputNotificationBufferBlock,
                    _processCommandTransformBlock);

                // Create a single bounded action block for each channel
                _outputActionBlocks[i] = new ActionBlock<BufferedEnvelope>(
                    async e => await SendToChannelAsync(channel, e.Envelope).ConfigureAwait(false),
                    new ExecutionDataflowBlockOptions() { BoundedCapacity = 1, MaxDegreeOfParallelism = 1 });

                var channelId = i;
                _outputBufferBlock.LinkTo(
                    _outputActionBlocks[i],
                    new DataflowLinkOptions() { PropagateCompletion = true},
                    e => e.ChannelId == channelId);

                _channels[i] = channel;
            }

            _semaphore = new SemaphoreSlim(1, 1);
        }

        public Task SendMessageAsync(Message message, CancellationToken cancellationToken)
            => SendToBufferAsync(message, cancellationToken);

        public Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken)
            => SendToBufferAsync(notification, cancellationToken);

        public Task SendCommandAsync(Command command, CancellationToken cancellationToken)
            => SendToBufferAsync(command, cancellationToken);

        public async Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            await EstablishIfRequiredAsync(cancellationToken).ConfigureAwait(false);
            return await _inputMessageBufferBlock.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<Notification> ReceiveNotificationAsync(CancellationToken cancellationToken)
        {
            await EstablishIfRequiredAsync(cancellationToken).ConfigureAwait(false);
            return await _inputNotificationBufferBlock.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<Command> ReceiveCommandAsync(CancellationToken cancellationToken)
        {
            await EstablishIfRequiredAsync(cancellationToken).ConfigureAwait(false);
            return await _inputCommandBufferBlock.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        }

        public Task<Command> ProcessCommandAsync(Command requestCommand, CancellationToken cancellationToken) 
            => _channelCommandProcessor.ProcessCommandAsync(this, requestCommand, cancellationToken);

        public bool IsEstablished => _channels.Any(c => c.IsEstablished);

        public async Task EstablishAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                for (var i = 0; i < _channels.Length; i++)
                {
                    _listeners[i].Start(_channels[i]);
                }

                await Task.WhenAll(
                    _channels.Select(c => c.EstablishAsync(cancellationToken)))
                    .ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public ICollection<Func<ChannelInformation, Task>> ChannelCreatedHandlers { get; }
        public ICollection<Func<ChannelInformation, Task>> ChannelDiscardedHandlers { get; }
        public ICollection<Func<FailedChannelInformation, Task<bool>>> ChannelCreationFailedHandlers { get; }
        public ICollection<Func<FailedChannelInformation, Task<bool>>> ChannelOperationFailedHandlers { get; }

        public async Task FinishAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _outputBufferBlock.Complete();

                await Task.WhenAll(_outputActionBlocks.Select(b => b.Completion))
                    .ConfigureAwait(false);

                for (var i = 0; i < _channels.Length; i++)
                {
                    _listeners[i].Stop();
                }

                _channelCommandProcessor.CancelAll();

                await Task.WhenAll(
                    _listeners.Select(
                        l => Task.WhenAll(
                            l.MessageListenerTask, 
                            l.NotificationListenerTask, 
                            l.CommandListenerTask)))
                    .ConfigureAwait(false);

                await Task.WhenAll(
                    _channels.Select(c => c.FinishAsync(cancellationToken)))
                    .ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void AttachCollection<T>(INotifyCollectionChanged observableCollection, ICollection<T> collection)
        {
            observableCollection.CollectionChanged += (sender, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (var item in e.NewItems.Cast<T>())
                    {
                        collection.Add(item);
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (var item in e.OldItems.Cast<T>())
                    {
                        collection.Remove(item);
                    }
                }
            };
        }

        private async Task EstablishIfRequiredAsync(CancellationToken cancellationToken)
        {
            if (!IsEstablished) await EstablishAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task SendToBufferAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            var channelId = Interlocked.Increment(ref _outputCounter) % _channels.Length;

            var bufferedEnvelope = new BufferedEnvelope(envelope, channelId);
            if (!await _outputBufferBlock.SendAsync(bufferedEnvelope, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The channel pipeline is complete");
            }
        }

        private async Task SendToChannelAsync(IOnDemandClientChannel channel, Envelope e)
        {
            try
            {
                if (e is Message) await channel.SendMessageAsync((Message) e);
                else if (e is Notification) await channel.SendNotificationAsync((Notification) e);
                else if (e is Command) await channel.SendCommandAsync((Command) e);
            }
            catch (Exception ex)
            {
                foreach (var handler in ChannelOperationFailedHandlers)
                {
                    await handler(
                        new FailedChannelInformation(
                            null,
                            channel.IsEstablished ? SessionState.Established : SessionState.Failed,
                            null,
                            null,
                            channel.IsEstablished,
                            ex,
                            nameof(SendToChannelAsync)));
                }
            }

        }

        public void Dispose()
        {
            _semaphore.Dispose();
            foreach (var channel in _channels)
            {
                channel.DisposeIfDisposable();
            }
        }

        private class BufferedEnvelope
        {
            public BufferedEnvelope(Envelope envelope, long channelId)
            {
                Envelope = envelope;
                ChannelId = channelId;
            }

            public Envelope Envelope { get; }

            public long ChannelId { get; }
        }
        
    }
}
