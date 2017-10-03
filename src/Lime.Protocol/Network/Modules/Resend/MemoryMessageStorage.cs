using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Util;

namespace Lime.Protocol.Network.Modules.Resend
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a memory message storage for resending messages.
    /// </summary>
    public sealed class MemoryMessageStorage : IMessageStorage
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MessageWithExpiration>> _channelMessageDictionary;
        private readonly ConcurrentDictionary<string, HashSet<Message>> _deadMessagesDictionary;

        public MemoryMessageStorage(ConcurrentDictionary<string, HashSet<Message>> deadMessagesDictionary = null)
        {
            _channelMessageDictionary = new ConcurrentDictionary<string, ConcurrentDictionary<string, MessageWithExpiration>>();
            _deadMessagesDictionary = deadMessagesDictionary;
        }

        public Task AddAsync(string channelKey, string messageKey, Message message, DateTimeOffset expiration, CancellationToken cancellationToken)
        {
            var messageDictionary = GetMessageDictionary(channelKey);
            messageDictionary[messageKey] = new MessageWithExpiration(message, expiration);
            return TaskUtil.CompletedTask;
        }

        public Task AddDeadMessageAsync(string channelKey, string messageKey, Message message, CancellationToken cancellationToken)
        {
            if (_deadMessagesDictionary != null)
            {
                var messageList = _deadMessagesDictionary.GetOrAdd(channelKey, (i) => new HashSet<Message>());
                messageList.Add(message);
            }

            return TaskUtil.CompletedTask;
        }

        public Task<IEnumerable<string>> GetExpiredMessageKeysAsync(string channelKey, CancellationToken cancellationToken) 
            => GetMessageDictionary(channelKey)
                .Where(m => m.Value.Expiration <= DateTimeOffset.UtcNow)
                .Select(m => m.Key)
                .AsCompletedTask();

        public Task<Message> RemoveAsync(string channelKey, string messageKey, CancellationToken cancellationToken)
        {
            var messageDictionary = GetMessageDictionary(channelKey);
            if (messageDictionary.TryRemove(messageKey, out var messageWithExpiration))
            {
                return messageWithExpiration.Message.AsCompletedTask();
            }

            return Task.FromResult<Message>(null);
        }

        private ConcurrentDictionary<string, MessageWithExpiration> GetMessageDictionary(string channelKey) 
            => _channelMessageDictionary.GetOrAdd(channelKey, k => new ConcurrentDictionary<string, MessageWithExpiration>());

        public sealed class MessageWithExpiration
        {
            public MessageWithExpiration(Message message, DateTimeOffset expiration)
            {
                Message = message ?? throw new ArgumentNullException(nameof(message));
                Expiration = expiration;
            }

            public Message Message { get; }

            public DateTimeOffset Expiration { get; }
        }
    }
}