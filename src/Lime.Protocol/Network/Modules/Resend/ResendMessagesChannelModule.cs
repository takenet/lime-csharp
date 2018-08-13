using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Client;

namespace Lime.Protocol.Network.Modules.Resend
{
    public class ResendMessagesChannelModule : IChannelModule<Message>, IChannelModule<Notification>, IDisposable
    {
        const string RESENT_COUNT_METADATA_KEY = "#resentCount";
        const string RESENT_SESSION_KEY = "#resentSession";
        const string RESENT_CHANNEL_ROLE_METADATA_KEY = "#resentChannelRole";

        private readonly IChannel _channel;
        private readonly IMessageStorage _messageStorage;
        private readonly IKeyProvider _keyProvider;
        private readonly IDeadMessageHandler _deadMessageHandler;
        private readonly int _maxResendCount;
        private readonly TimeSpan _resendWindow;
        private readonly Event[] _eventsToRemovePendingMessage;
        private readonly object _syncRoot = new object();
        private readonly Func<Exception, IChannel, Message, Task> _resendExceptionHandler;

        private string _channelKey;
        private Task _resendTask;
        private CancellationTokenSource _cts;

        public ResendMessagesChannelModule(
            IChannel channel, 
            IMessageStorage messageStorage, 
            IKeyProvider keyProvider,       
            IDeadMessageHandler deadMessageHandler,
            int maxResendCount, 
            TimeSpan resendWindow,
            Event[] eventsToRemovePendingMessage = null,
            Func<Exception, IChannel, Message, Task> resendExceptionHandler = null)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _messageStorage = messageStorage ?? throw new ArgumentNullException(nameof(messageStorage));
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
            _deadMessageHandler = deadMessageHandler ?? throw new ArgumentNullException(nameof(deadMessageHandler));
            _maxResendCount = maxResendCount;
            _resendWindow = resendWindow;
            if (eventsToRemovePendingMessage != null && eventsToRemovePendingMessage.Length == 0)
            {
                throw new ArgumentException("At least one event must be provided", nameof(eventsToRemovePendingMessage));
            }
            _eventsToRemovePendingMessage = eventsToRemovePendingMessage;
            _resendExceptionHandler = resendExceptionHandler;
        }

        public virtual void OnStateChanged(SessionState state)
        {
            lock (_syncRoot)
            {
                if (state == SessionState.Established)
                {
                    if (_resendTask == null) StartResendTask();
                }
                else if (state > SessionState.Established)
                {
                    if (_resendTask != null) StopResendTask();
                }
            }
        }

        public virtual async Task<Message> OnSendingAsync(Message envelope, CancellationToken cancellationToken)
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

        public virtual async Task<Notification> OnReceivingAsync(Notification envelope, CancellationToken cancellationToken)
        {
            if (_eventsToRemovePendingMessage != null 
                && !_eventsToRemovePendingMessage.Contains(envelope.Event))
            {
                return envelope;
            }

            var messageKey = _keyProvider.GetMessageKey(envelope, _channel);
            await _messageStorage.RemoveAsync(_channelKey, messageKey, cancellationToken);
            return envelope;
        }

        public virtual Task<Notification> OnSendingAsync(Notification envelope, CancellationToken cancellationToken) 
            => envelope.AsCompletedTask();

        public virtual Task<Message> OnReceivingAsync(Message envelope, CancellationToken cancellationToken) 
            => envelope.AsCompletedTask();

        /// <summary>
        /// RegisterDocument the module to the specified channel.
        /// </summary>
        /// <param name="channel"></param>
        public virtual void RegisterTo(IChannel channel)
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
            IKeyProvider keyProvider = null,
            IDeadMessageHandler deadMessageHandler = null,
            Event[] eventsToRemovePendingMessage = null)
        {
            var resendMessagesChannelModule = new ResendMessagesChannelModule(
                channel, 
                messageStorage ?? new MemoryMessageStorage(), 
                keyProvider ?? KeyProvider.Instance,
                deadMessageHandler ?? DiscardDeadMessageHandler.Instance,
                maxResendCount, 
                resendWindow,
                eventsToRemovePendingMessage);
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
                _resendTask = null;
            }
            catch (ObjectDisposedException) { }
        }

        private async Task ResendExpiredMessagesAsync(CancellationToken cancellationToken)
        {            
            while (!cancellationToken.IsCancellationRequested && _channel.State == SessionState.Established && _channel.Transport.IsConnected)
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
                            catch (Exception ex)
                            {
                                Trace.TraceError("Error resending a message with id {0} on channel {1}: {2}", expiredMessage.Id, _channel.SessionId, ex.ToString());
                                if (_resendExceptionHandler != null)
                                {
                                    await _resendExceptionHandler(ex, _channel, expiredMessage);
                                }

                                // Create a new CTS because the exception can be caused by the cancellation of the method token
                                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                                {
                                    try
                                    {
                                        // If any error occurs when resending the message, put the expired message
                                        // back into the storage before throwing the exception.
                                        await _messageStorage.AddAsync(_channelKey, expiredMessageKey, expiredMessage,
                                            DateTimeOffset.UtcNow.Add(_resendWindow), cts.Token);
                                    }
                                    catch (OperationCanceledException) { }
                                    throw;
                                }
                            }
                        }
                        else
                        {
                            await _deadMessageHandler.HandleDeadMessageAsync(expiredMessage, _channel, cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("An unhandled exception occurred on ResendMessagesChannelModule on channel {0}: {1}", _channel.SessionId, ex.ToString());
                    if (_resendExceptionHandler != null)
                    {
                        await _resendExceptionHandler(ex, _channel, null);
                    }

                    if (_channel.State != SessionState.Established || !_channel.Transport.IsConnected) break;
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

        private void SetMessageResendCount(Message expiredMessage, int resendCount)
        {
            if (expiredMessage.Metadata == null) expiredMessage.Metadata = new Dictionary<string, string>();
            expiredMessage.Metadata[RESENT_COUNT_METADATA_KEY] = resendCount.ToString();
            expiredMessage.Metadata[RESENT_SESSION_KEY] = _channel.SessionId;
            expiredMessage.Metadata[RESENT_CHANNEL_ROLE_METADATA_KEY] = _channel is IClientChannel ? "Client" : "Server";
        }

        public void Dispose()
        {
            StopResendTask();
        }
    }
}