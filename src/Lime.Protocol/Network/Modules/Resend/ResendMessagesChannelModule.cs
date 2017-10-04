using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network.Modules.Resend
{
    public sealed class ResendMessagesChannelModule : IChannelModule<Message>, IChannelModule<Notification>, IDisposable
    {
        const string RESENT_COUNT_METADATA_KEY = "#resentCount";

        private readonly IChannel _channel;
        private readonly IMessageStorage _messageStorage;
        private readonly IKeyProvider _keyProvider;
        private readonly int _maxResendCount;
        private readonly TimeSpan _resendWindow;
        private readonly object _syncRoot = new object();

        private string _channelKey;
        private Task _resendTask;
        private CancellationTokenSource _cts;

        public ResendMessagesChannelModule(
            IChannel channel, 
            IMessageStorage messageStorage, 
            IKeyProvider keyProvider,            
            int maxResendCount, 
            TimeSpan resendWindow)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _messageStorage = messageStorage ?? throw new ArgumentNullException(nameof(messageStorage));
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
            _maxResendCount = maxResendCount;
            _resendWindow = resendWindow;
        }

        public void OnStateChanged(SessionState state)
        {
            lock (_syncRoot)
            {
                if (state == SessionState.Established)
                {
                    StartResendTask();
                }
                else if (state > SessionState.Established)
                {
                    StopResendTask();
                }
            }
        }

        public async Task<Message> OnSendingAsync(Message envelope, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(envelope.Id)) return envelope;
            var messageKey = _keyProvider.GetMessageKey(envelope, _channel);

            var resendCount = GetMessageResendCount(envelope);

            await _messageStorage.AddAsync(
                _channelKey, 
                messageKey, 
                envelope, 
                DateTimeOffset.UtcNow.AddTicks(_resendWindow.Ticks * (resendCount + 1)), 
                cancellationToken);
            return envelope;
        }

        public async Task<Notification> OnReceivingAsync(Notification envelope, CancellationToken cancellationToken)
        {
            var messageKey = _keyProvider.GetMessageKey(envelope, _channel);
            await _messageStorage.RemoveAsync(_channelKey, messageKey, cancellationToken);
            return envelope;
        }

        public Task<Notification> OnSendingAsync(Notification envelope, CancellationToken cancellationToken) 
            => envelope.AsCompletedTask();

        public Task<Message> OnReceivingAsync(Message envelope, CancellationToken cancellationToken) 
            => envelope.AsCompletedTask();

        /// <summary>
        /// Register the module to the specified channel.
        /// </summary>
        /// <param name="channel"></param>
        public void RegisterTo(IChannel channel)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            channel.MessageModules.Add(this);
            channel.NotificationModules.Add(this);
        }

        public static ResendMessagesChannelModule CreateAndRegister(
            IChannel channel, 
            int maxResendCount, 
            TimeSpan resendWindow, 
            IMessageStorage messageStorage = null, 
            IKeyProvider keyProvider = null)
        {
            var resendMessagesChannelModule = new ResendMessagesChannelModule(
                channel, messageStorage ?? new MemoryMessageStorage(), keyProvider ?? new KeyProvider(), maxResendCount, resendWindow);
            resendMessagesChannelModule.RegisterTo(channel);
            return resendMessagesChannelModule;
        }

        private void StartResendTask()
        {
            _channelKey = _keyProvider.GetChannelKey(_channel);
            _cts = new CancellationTokenSource();
            _resendTask = Task.Run(() => ResendExpiredMessagesAsync(_cts.Token));
        }

        private void StopResendTask()
        {
            try
            {
                _cts?.Cancel();
                _resendTask?.GetAwaiter().GetResult();
                _cts?.Dispose();
            }
            catch (ObjectDisposedException) { }
        }

        private async Task ResendExpiredMessagesAsync(CancellationToken cancellationToken)
        {            
            while (!cancellationToken.IsCancellationRequested)
            {                
                try
                {
                    await Task.Delay(_resendWindow, cancellationToken);

                    var expiredMessageKeys =
                        await _messageStorage.GetMessagesToResendKeysAsync(_channelKey, DateTimeOffset.UtcNow, cancellationToken);

                    foreach (var expiredMessageKey in expiredMessageKeys)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var expiredMessage = await _messageStorage.RemoveAsync(_channelKey, expiredMessageKey, cancellationToken);
                        if (expiredMessage == null) continue; // It may be already removed
                        
                        var resendCount = GetMessageResendCount(expiredMessage);

                        // Check the message resend limit
                        if (resendCount < _maxResendCount)
                        {
                            // Set the metadata key
                            resendCount++;
                            SetMessageResendCount(expiredMessage, resendCount);

                            try
                            {
                                await _channel.SendMessageAsync(expiredMessage, cancellationToken);
                            }
                            catch
                            {
                                // Create a new CTS because the exception can be caused by the cancellation of the method token
                                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                                {
                                    try
                                    {
                                        // If any error occurs when resending the message, put the expired message
                                        // back into the storage before throwing the exception.
                                        await _messageStorage.AddAsync(_channelKey, expiredMessageKey, expiredMessage,
                                            DateTimeOffset.UtcNow, cts.Token);
                                    }
                                    catch (OperationCanceledException) { }
                                    throw;
                                }
                            }
                        }
                        else
                        {
                            await _messageStorage.AddDeadMessageAsync(_channelKey, expiredMessageKey, expiredMessage,
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
                        StopResendTask();
                        break;
                    }
                    throw;
                }
            }
        }

        private static int GetMessageResendCount(Message expiredMessage)
        {
            if (expiredMessage.Metadata != null 
                && expiredMessage.Metadata.TryGetValue(RESENT_COUNT_METADATA_KEY, out var resendCountValue) 
                && int.TryParse(resendCountValue, out var resendCount)) return resendCount;
            return 0;
        }

        private static void SetMessageResendCount(Message expiredMessage, int resendCount)
        {
            if (expiredMessage.Metadata == null) expiredMessage.Metadata = new Dictionary<string, string>();
            expiredMessage.Metadata[RESENT_COUNT_METADATA_KEY] = resendCount.ToString();
        }

        public void Dispose()
        {
            StopResendTask();
        }
    }
}