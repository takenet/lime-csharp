using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lime.Protocol.Network.Modules
{
    /// <summary>
    /// Defines a module that resend messages that doesn't have <see cref="Event.Received"/> receipts from the destination.
    /// </summary>
    public sealed class ResendMessagesChannelModule : IChannelModule<Message>, IChannelModule<Notification>
    {        
        private readonly int _resendMessageTryCount;
        private readonly TimeSpan _resendMessageInterval;
        private readonly bool _filterByDestination;
        private readonly ConcurrentDictionary<MessageIdDestination, SentMessage> _sentMessageDictionary;
        private readonly object _syncRoot = new object();        
        private readonly BufferBlock<SentMessage> _inputBlock;
        private readonly TransformBlock<SentMessage, SentMessage> _waitForRetryBlock;
        private readonly ActionBlock<SentMessage> _resendBlock;

        private IChannel _channel;
        private bool _unbindWhenClosed;
        private IDisposable _link;

        public ResendMessagesChannelModule(int resendMessageTryCount, TimeSpan resendMessageInterval, bool filterByDestination = false)
        {            
            if (resendMessageTryCount <= 0) throw new ArgumentOutOfRangeException(nameof(resendMessageTryCount));            
            _resendMessageTryCount = resendMessageTryCount;
            _resendMessageInterval = resendMessageInterval;
            _filterByDestination = filterByDestination;

            _sentMessageDictionary = new ConcurrentDictionary<MessageIdDestination, SentMessage>();            
            _inputBlock = new BufferBlock<SentMessage>();
            _waitForRetryBlock =  new TransformBlock<SentMessage, SentMessage>(
                m => WaitForRetryAsync(m),
                new ExecutionDataflowBlockOptions() { BoundedCapacity = DataflowBlockOptions.Unbounded});
            _resendBlock = new ActionBlock<SentMessage>(
                ResendMessageAsync, 
                new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });
            _inputBlock.LinkTo(_waitForRetryBlock);
        }

        private async Task<SentMessage> WaitForRetryAsync(SentMessage sentMessage)
        {                                        
            try
            {
                await Task.Delay(_resendMessageInterval, sentMessage.CancellationToken);
                return sentMessage;
            }            
            catch (OperationCanceledException) when (sentMessage.CancellationToken.IsCancellationRequested)
            {
                return null;
            }                       
        }

        private async Task ResendMessageAsync(SentMessage sentMessage)
        {
            if (sentMessage == null || sentMessage.CancellationToken.IsCancellationRequested) return;
            
            try
            {
                await _channel.SendMessageAsync(sentMessage.Message);
            }
            catch
            {
                Unbind();
            }            
        }

        public void OnStateChanged(SessionState state)
        {            
            if (state > SessionState.Established &&
                IsBound &&
                _unbindWhenClosed)
            {
                Unbind();
            }            
        }

        Task<Notification> IChannelModule<Notification>.OnReceivingAsync(Notification envelope, CancellationToken cancellationToken)
        {            
            if (envelope.Event == Event.Received || envelope.Event == Event.Failed)
            {                
                SentMessage sentMessage;
                if (_sentMessageDictionary.TryRemove(Create(envelope), out sentMessage))
                {
                    sentMessage.Dispose();
                }
            }

            return envelope.AsCompletedTask();
        }

        Task<Notification> IChannelModule<Notification>.OnSendingAsync(Notification envelope, CancellationToken cancellationToken)
        {
            return envelope.AsCompletedTask();
        }

        Task<Message> IChannelModule<Message>.OnReceivingAsync(Message envelope, CancellationToken cancellationToken)
        {
            return envelope.AsCompletedTask();
        }

        async Task<Message> IChannelModule<Message>.OnSendingAsync(Message envelope, CancellationToken cancellationToken)
        {
            if (envelope.Id != Guid.Empty)
            {
                var key = Create(envelope);
                SentMessage sentMessage;
                if (_sentMessageDictionary.TryGetValue(key, out sentMessage))
                {
                    sentMessage.IncrementSentCount();
                }
                else
                {
                    sentMessage = new SentMessage(envelope);
                    _sentMessageDictionary.TryAdd(Create(envelope), sentMessage);
                }

                if (sentMessage.ResentCount <= _resendMessageTryCount)
                {
                    await _inputBlock.SendAsync(sentMessage, cancellationToken);
                }
                else
                {
                    _sentMessageDictionary.TryRemove(key, out sentMessage);
                }                                
            }

            return envelope;
        }

        public bool IsBound => _channel != null;

        public void Bind(IChannel channel, bool unbindWhenClosed)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));

            lock (_syncRoot)
            {                
                if (IsBound) throw new InvalidOperationException("The module is already bound to a channel. Call Unbind first.");
                _channel = channel;
                _unbindWhenClosed = unbindWhenClosed;
                _channel.MessageModules.Add(this);
                _channel.NotificationModules.Add(this);
                _link = _waitForRetryBlock.LinkTo(_resendBlock);
            }
        }

        public void Unbind()
        {
            lock (_syncRoot)
            {
                if (!IsBound) throw new InvalidOperationException("The module is not bound to a channel.");
                _channel.MessageModules.Remove(this);
                _channel.NotificationModules.Remove(this);
                _channel = null;
                _link.Dispose();
            }
        }

        private MessageIdDestination Create(Message message)
        {
            return new MessageIdDestination(
                message.Id, 
                _filterByDestination ? (message.To ?? _channel.RemoteNode).ToIdentity() : null);
        }

        private MessageIdDestination Create(Notification notification)
        {
            return new MessageIdDestination(
                notification.Id, 
                _filterByDestination ? 
                (notification.GetSender() ?? _channel.RemoteNode).ToIdentity() : null);
        }

        private sealed class MessageIdDestination
        {
            private readonly Guid _messageId;
            private readonly Identity _destination;
            
            public MessageIdDestination(Guid messageId, Identity destination = null)
            {
                _messageId = messageId;
                _destination = destination;                
            }

            public override bool Equals(object obj)
            {
                return ToString().Equals(obj.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            public override int GetHashCode()
            {
                return ToString().GetHashCode();
            }            

            public override string ToString()
            {
                return _destination == null ? 
                    _messageId.ToString() : 
                    $"{_messageId}:{_destination}";
            }            
        }

        private sealed class SentMessage : IDisposable
        {
            const string RESENT_COUNT_KEY = "#resentCount";

            private readonly Message _message;            
            private readonly CancellationTokenSource _cts;

            public SentMessage(Message message)
                : this(message, 1)
            {

            }

            private SentMessage(Message message, int resentCount)
            {
                if (message == null) throw new ArgumentNullException(nameof(Message));
                _message = message;                
                ResentCount = resentCount;
                _cts = new CancellationTokenSource();
            }

            public Message Message
            {
                get
                {
                    if (_message.Metadata == null) _message.Metadata = new Dictionary<string, string>();
                    _message.Metadata.Remove(RESENT_COUNT_KEY);
                    _message.Metadata.Add(RESENT_COUNT_KEY, ResentCount.ToString());
                    return _message;
                }
            }            

            public int ResentCount { get; private set; }

            public CancellationToken CancellationToken => _cts.Token;

            public void IncrementSentCount()
            {
                ResentCount++;
            }

            public void Dispose()
            {
                _cts.Cancel();
                _cts.Dispose();
            }
        }    
    }
}
