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
    public sealed class MultiplexerClientChannel : IOnDemandClientChannel, IDisposable
    {
        private readonly IOnDemandClientChannel[] _channels;
        private readonly IChannelListener[] _listeners;
        private readonly SemaphoreSlim _semaphore;
        private readonly BufferBlock<Envelope> _outputBufferBlock;
        private readonly ActionBlock<Envelope>[] _outputActionBlocks;
        private readonly BufferBlock<Message> _inputMessageBufferBlock;
        private readonly BufferBlock<Notification> _inputNotificationBufferBlock;
        private readonly BufferBlock<Command> _inputCommandBufferBlock;

        public MultiplexerClientChannel(
            EstablishedClientChannelBuilder builder, 
            int count = 5,
            int inputBoundedCapacity = DataflowBlockOptions.Unbounded,
            int outputBoundedCapacity = DataflowBlockOptions.Unbounded)
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

            var inputOptions = new DataflowBlockOptions()
            {
                BoundedCapacity = inputBoundedCapacity
            };

            // Global input buffers
            _inputMessageBufferBlock = new BufferBlock<Message>(inputOptions);
            _inputNotificationBufferBlock = new BufferBlock<Notification>(inputOptions);
            _inputCommandBufferBlock = new BufferBlock<Command>(inputOptions);

            // An output action block per channel
            _outputBufferBlock = new BufferBlock<Envelope>(new DataflowBlockOptions()
            {
                BoundedCapacity = outputBoundedCapacity
            });
            _outputActionBlocks = new ActionBlock<Envelope>[count];

            _channels = new IOnDemandClientChannel[count];
            for (var i = 0; i < _channels.Length; i++)
            {
                var currentBuilder = builder
                    .ShallowCopy()
                    .WithInstance($"{builder.Instance}-{i}");
                _channels[i] = new OnDemandClientChannel(currentBuilder);
                var channel = _channels[i];

                // Synchronize the handlers
                SynchronizeCollections(channelCreatedHandlers, channel.ChannelCreatedHandlers);
                SynchronizeCollections(channelDiscardedHandlers, channel.ChannelDiscardedHandlers);
                SynchronizeCollections(channelCreationFailedHandlers, channel.ChannelCreationFailedHandlers);
                SynchronizeCollections(channelOperationFailedHandlers, channel.ChannelOperationFailedHandlers);

                // Setup the listener for the channel
                _listeners[i] = new DataflowChannelListener(
                    _inputMessageBufferBlock,
                    _inputNotificationBufferBlock,
                    _inputCommandBufferBlock);

                // Create a single bounded block for each channel
                _outputActionBlocks[i] = new ActionBlock<Envelope>(async e =>
                {
                    if (e is Message) await channel.SendMessageAsync((Message) e);
                    else if (e is Notification) await channel.SendNotificationAsync((Notification)e);                    
                    else if (e is Command) await channel.SendCommandAsync((Command)e);
                    else if (e is RequestResponseCommand)
                    {
                        var requestResponseCommand = (RequestResponseCommand) e;
                        try
                        {
                            var response =
                                await
                                    channel.ProcessCommandAsync(requestResponseCommand.Request,
                                        requestResponseCommand.CancellationToken);
                            requestResponseCommand.ResponseTcs.TrySetResult(response);
                        }
                        catch(OperationCanceledException) when (requestResponseCommand.CancellationToken.IsCancellationRequested) { }
                        catch (Exception ex)
                        {
                            requestResponseCommand.ResponseTcs.TrySetException(ex);
                        }
                    }
                },
                new ExecutionDataflowBlockOptions() { BoundedCapacity = 1, MaxDegreeOfParallelism = 1});
                _outputBufferBlock.LinkTo(_outputActionBlocks[i]);
            }

            _semaphore = new SemaphoreSlim(1, 1);
        }

        public Task SendMessageAsync(Message message, CancellationToken cancellationToken)
            => SendAsync(message, cancellationToken);

        public Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken)
            => SendAsync(notification, cancellationToken);

        public Task SendCommandAsync(Command command, CancellationToken cancellationToken)
            => SendAsync(command, cancellationToken);

        public Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
            => _inputMessageBufferBlock.ReceiveAsync(cancellationToken);

        public Task<Notification> ReceiveNotificationAsync(CancellationToken cancellationToken)
            => _inputNotificationBufferBlock.ReceiveAsync(cancellationToken);

        public Task<Command> ReceiveCommandAsync(CancellationToken cancellationToken)
            => _inputCommandBufferBlock.ReceiveAsync(cancellationToken);

        public async Task<Command> ProcessCommandAsync(Command requestCommand, CancellationToken cancellationToken)
        {
            var requestResponseCommand = new RequestResponseCommand(requestCommand, cancellationToken);
            await SendAsync(requestResponseCommand, cancellationToken).ConfigureAwait(false);
            using (cancellationToken.Register(() => requestResponseCommand.ResponseTcs.TrySetCanceled(cancellationToken)))
            {
                return await requestResponseCommand.ResponseTcs.Task.ConfigureAwait(false);
            }
        }

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
                for (var i = 0; i < _channels.Length; i++)
                {
                    _listeners[i].Stop();
                }

                await Task.WhenAll(
                    _channels.Select(c => c.FinishAsync(cancellationToken)))
                    .ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void SynchronizeCollections<T>(INotifyCollectionChanged observableCollection, ICollection<T> collection)
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

        private async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (!await _outputBufferBlock.SendAsync(envelope, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The channel pipeline is complete");
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

        private class RequestResponseCommand : Envelope
        {
            public RequestResponseCommand(Command request, CancellationToken cancellationToken)
            {
                Request = request;
                CancellationToken = cancellationToken;
                ResponseTcs = new TaskCompletionSource<Command>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public Command Request { get; }

            public CancellationToken CancellationToken { get; }

            public TaskCompletionSource<Command> ResponseTcs { get;  }
        }
        
    }
}
