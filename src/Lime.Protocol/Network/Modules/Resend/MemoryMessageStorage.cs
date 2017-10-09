using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly TimeSpan _resendExpiration;
        private readonly int _checkForRemovalInterval;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MessageToResend>> _channelMessageDictionary;
        private long _addCount;

        public MemoryMessageStorage()
            : this(TimeSpan.FromMinutes(10), 5)
        {
            
        }

        /// <summary>
        /// Initialize a new instance of <see cref="MemoryMessageStorage"/>.
        /// </summary>
        /// <param name="resendExpiration">The interval to remove messages to resend for storage.</param>
        /// <param name="checkForRemovalInterval">The interval for checking for messages to removal.</param>
        public MemoryMessageStorage(TimeSpan resendExpiration, int checkForRemovalInterval)
        {
            if (resendExpiration <= TimeSpan.Zero) throw new ArgumentException("The resend expiration value must be positive", nameof(resendExpiration));
            if (checkForRemovalInterval <= 0) throw new ArgumentOutOfRangeException(nameof(checkForRemovalInterval));

            _resendExpiration = resendExpiration;
            _checkForRemovalInterval = checkForRemovalInterval;
            _channelMessageDictionary = new ConcurrentDictionary<string, ConcurrentDictionary<string, MessageToResend>>();
        }

        public Task AddAsync(string channelKey, string messageKey, Message message, DateTimeOffset resendAt, CancellationToken cancellationToken)
        {
            // Try remove pending messages for each number of configured messages
            if (++_addCount % _checkForRemovalInterval == 0)
            {
                RemoveOldPendingMessages();
            }

            var messageDictionary = GetMessageDictionary(channelKey);
            messageDictionary[messageKey] = new MessageToResend(message, resendAt);
            return TaskUtil.CompletedTask;
        }

        public Task<IEnumerable<string>> GetMessagesToResendKeysAsync(string channelKey, DateTimeOffset reference, CancellationToken cancellationToken) 
            => GetMessageDictionary(channelKey)
                .Where(m => m.Value.ResendAt <= reference)
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

        private ConcurrentDictionary<string, MessageToResend> GetMessageDictionary(string channelKey) 
            => _channelMessageDictionary.GetOrAdd(channelKey, k => new ConcurrentDictionary<string, MessageToResend>());

        private void RemoveOldPendingMessages()
        {
            var reference = DateTimeOffset.UtcNow.AddTicks(_resendExpiration.Ticks * -1);

            var expiredChannelKeys = new List<string>();

            foreach (var keyValuePair in _channelMessageDictionary)
            {
                var messageDictionary = keyValuePair.Value;

                var expiredKeys = messageDictionary
                    .Where(m => m.Value.ResendAt <= reference)
                    .Select(m => m.Key);

                foreach (var expiredKey in expiredKeys)
                {
                    messageDictionary.TryRemove(expiredKey, out _);
                }

                if (messageDictionary.IsEmpty) expiredChannelKeys.Add(keyValuePair.Key);
            }

            foreach (var expiredChannelKey in expiredChannelKeys)
            {
                _channelMessageDictionary.TryRemove(expiredChannelKey, out _);
            }
        }

        public sealed class MessageToResend
        {
            public MessageToResend(Message message, DateTimeOffset resendAt)
            {
                Message = message ?? throw new ArgumentNullException(nameof(message));
                ResendAt = resendAt;
            }

            public Message Message { get; }

            public DateTimeOffset ResendAt { get; }
        }
    }
}