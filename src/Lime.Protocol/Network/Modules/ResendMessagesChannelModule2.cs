using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network.Modules
{
    public class ResendMessagesChannelModule2 : IChannelModule<Message>, IChannelModule<Notification>, IDisposable
    {
        const string RESENT_COUNT_METADATA_KEY = "#resentCount";

        private readonly IChannel _channel;
        private readonly IMessageStorage _messageStorage;
        private readonly int _maxResendCount;
        private readonly TimeSpan _resendWindow;
        private readonly object _syncRoot = new object();

        private string _channelKey;
        private Task _resendTask;
        private CancellationTokenSource _cts;

        protected ResendMessagesChannelModule2(IChannel channel, IMessageStorage messageStorage, int maxResendCount, TimeSpan resendWindow)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _messageStorage = messageStorage ?? throw new ArgumentNullException(nameof(messageStorage));
            _maxResendCount = maxResendCount;
            _resendWindow = resendWindow;
        }

        public virtual void OnStateChanged(SessionState state)
        {
            lock (_syncRoot)
            {
                if (state == SessionState.Established)
                {
                    _channelKey = GetChannelKey(_channel);
                    _cts = new CancellationTokenSource();
                    _resendTask = Task.Run(() => ResendExpiredMessagesAsync(_cts.Token));
                }
                else if (state > SessionState.Established)
                {
                    _cts?.Cancel();
                    _resendTask?.GetAwaiter().GetResult();
                }
            }
        }        

        public async Task<Message> OnSendingAsync(Message envelope, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(envelope.Id)) return envelope;
            var messageKey = GetMessageKey(envelope, _channel);
            await _messageStorage.AddAsync(_channelKey, messageKey, envelope, DateTimeOffset.UtcNow.Add(_resendWindow), cancellationToken);
            return envelope;
        }

        public Task<Notification> OnReceivingAsync(Notification envelope, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Notification> OnSendingAsync(Notification envelope, CancellationToken cancellationToken) 
            => envelope.AsCompletedTask();

        public Task<Message> OnReceivingAsync(Message envelope, CancellationToken cancellationToken) 
            => envelope.AsCompletedTask();

        public static ResendMessagesChannelModule2 CreateAndRegister(IChannel channel, IMessageStorage messageStorage, int maxResendCount, TimeSpan resendWindow)
        {
            var resendMessagesChannelModule = new ResendMessagesChannelModule2(channel, messageStorage, maxResendCount, resendWindow);
            channel.MessageModules.Add(resendMessagesChannelModule);
            channel.NotificationModules.Add(resendMessagesChannelModule);
            return resendMessagesChannelModule;
        }

        /// <summary>
        /// Defines the channel key using the local and remote nodes.
        /// The channel key should be the same when a channel disconnects and reconnects with the same instance.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        private static string GetChannelKey(IChannel channel) => $"{channel.RemoteNode.ToIdentity()}:{channel.LocalNode}".ToLowerInvariant();

        private static string GetMessageKey(Message message, IChannel channel) => $"{(message.To ?? channel.RemoteNode).ToIdentity()}:{message.Id}".ToLowerInvariant();

        private async Task ResendExpiredMessagesAsync(CancellationToken cancellationToken)
        {            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var expiredMessageKeys =
                        await _messageStorage.GetExpiredMessageKeysAsync(_channelKey, _cts.Token);

                    foreach (var expiredMessageKey in expiredMessageKeys)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var expiredMessage = await _messageStorage.RemoveAsync(_channelKey, expiredMessageKey, cancellationToken);
                        if (expiredMessage == null) continue; // It may be already removed
                        
                        var resendCount = 0;

                        // Check the message resend limit
                        if (expiredMessage.Metadata == null 
                            || !expiredMessage.Metadata.TryGetValue(RESENT_COUNT_METADATA_KEY, out var resendCountValue) 
                            || !int.TryParse(resendCountValue, out resendCount) 
                            || resendCount < _maxResendCount)
                        {
                            // Set the metadata key
                            resendCount++;
                            if (expiredMessage.Metadata == null) expiredMessage.Metadata = new Dictionary<string, string>();
                            expiredMessage.Metadata[RESENT_COUNT_METADATA_KEY] = resendCount.ToString();

                            try
                            {
                                await _channel.SendMessageAsync(expiredMessage, cancellationToken);
                            }
                            catch
                            {
                                // Create a new CTS because the exception can be caused by the cancellation of the method token
                                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                                {
                                    // If any error occurs when resending the message, put the expired message
                                    // back into the storage before throwing the exception.
                                    await _messageStorage.AddAsync(_channelKey, expiredMessageKey, expiredMessage,
                                        DateTimeOffset.UtcNow, cts.Token);

                                    throw;
                                }
                            }
                        }
                        else
                        {
                            await _messageStorage.AddExpiredMessage(_channelKey, expiredMessageKey, expiredMessage,
                                cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
#if !NETSTANDARD1_1
                    Trace.TraceError(ex.ToString());
#endif

                    if (_channel.State != SessionState.Established || !_channel.Transport.IsConnected)
                    {
                        break;
                    }
                    throw;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public interface IMessageStorage
    {
        Task AddAsync(string channelkey, string messageKey, Message message, DateTimeOffset expiration, CancellationToken cancellationToken);

        Task<Message> RemoveAsync(string channelkey, string messageKey, CancellationToken cancellationToken);

        Task<IEnumerable<string>> GetExpiredMessageKeysAsync(string channelKey, CancellationToken cancellationToken);

        Task AddExpiredMessage(string channelkey, string messageKey, Message message, CancellationToken cancellationToken);
    }
}