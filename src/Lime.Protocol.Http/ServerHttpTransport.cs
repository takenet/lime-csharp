using Lime.Protocol.Http.Storage;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lime.Protocol.Http
{
    /// <summary>
    /// Represents the server transport 
    /// for the HTTP emulation layer.
    /// </summary>
    public class ServerHttpTransport : TransportBase
    {
        #region Private Fields        

        private HttpListenerBasicIdentity _httpIdentity;
        private readonly BufferBlock<Envelope> _inputBufferBlock;

        private readonly IEnvelopeStorage<Message> _messageStorage;
        private readonly IEnvelopeStorage<Notification> _notificationStorage;
        
        private readonly TaskCompletionSource<Session> _sessionTaskCompletionSource;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Notification>> _pendingNotificationsDictionary;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Command>> _pendingCommandsDictionary;

        #endregion

        #region Constructor

        internal ServerHttpTransport(HttpListenerBasicIdentity identity, bool isHttps, IEnvelopeStorage<Message> messageStorage, IEnvelopeStorage<Notification> notificationStorage)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }

            _httpIdentity = identity;
            Compression = SessionCompression.None;
            Encryption = isHttps ? SessionEncryption.TLS : SessionEncryption.None;

            _messageStorage = messageStorage;
            _notificationStorage = notificationStorage;

            _inputBufferBlock = new BufferBlock<Envelope>();

            _sessionTaskCompletionSource = new TaskCompletionSource<Session>();
            _pendingNotificationsDictionary = new ConcurrentDictionary<Guid, TaskCompletionSource<Notification>>();
            _pendingCommandsDictionary = new ConcurrentDictionary<Guid, TaskCompletionSource<Command>>();
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Adds the envelope to the buffer that
        /// the server reads the envelopes
        /// sent by the node.
        /// </summary>
        internal Task<bool> SendToInputBufferAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            return _inputBufferBlock.SendAsync(envelope, cancellationToken);
        }

        internal async Task<Notification> ProcessMessageAsync(Message message, CancellationToken cancellationToken)
        {
            if (message.Id == Guid.Empty)
            {
                throw new ArgumentException("Invalid message id");
            }

            var tcs = new TaskCompletionSource<Notification>();
            cancellationToken.Register(() => tcs.TrySetCanceled());
            _pendingNotificationsDictionary.TryAdd(message.Id, tcs);
            if (!await SendToInputBufferAsync(message, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The input buffer is complete");
            }

            return await tcs.Task;
        }

        internal async Task<Command> ProcessCommandAsync(Command command, CancellationToken cancellationToken)
        {
            if (command.Id == Guid.Empty)
            {
                throw new ArgumentException("Invalid command id");
            }

            var tcs = new TaskCompletionSource<Command>();
            cancellationToken.Register(() => tcs.TrySetCanceled());
            _pendingCommandsDictionary.TryAdd(command.Id, tcs);
            if (!await SendToInputBufferAsync(command, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The input buffer is complete");
            }

            return await tcs.Task;
        }

        internal Task<Session> AuthenticateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _sessionTaskCompletionSource.TrySetCanceled());
            return _sessionTaskCompletionSource.Task;
        }

        /// <summary>
        /// Gets the transport associated identity.
        /// </summary>
        internal HttpListenerBasicIdentity HttpIdentity
        {
            get { return _httpIdentity; }
        }

        #endregion

        #region TransportBase Members

        public override SessionCompression[] GetSupportedCompression()
        {
            return new[] { SessionCompression.None };
        }

        public override SessionEncryption[] GetSupportedEncryption()
        {
            return new[] { Encryption };
        }

        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope is Session)
            {
                // The session negotiation is emulated by the transport
                var session = (Session)envelope;
                await ReceiveSessionAsync(cancellationToken, session).ConfigureAwait(false);
            }
            else if (envelope is Notification)
            {
                var notification = (Notification)envelope;

                TaskCompletionSource<Notification> notificationTcs;
                if (_pendingNotificationsDictionary.TryRemove(envelope.Id, out notificationTcs))
                {
                    notificationTcs.TrySetResult(notification);                                       
                }
                else
                {
                    var owner = notification.To.ToIdentity();
                    var existingNotification = await _notificationStorage.GetEnvelopeAsync(owner, envelope.Id).ConfigureAwait(false);
                    if (existingNotification != null)
                    {
                        if (notification.Event == Event.Failed ||
                            existingNotification.Event < notification.Event)
                        {
                            bool updated = false;
                            int tryCount = 0;

                            while (!updated && tryCount++ < 3)
                            {
                                await _notificationStorage.DeleteEnvelopeAsync(owner, envelope.Id).ConfigureAwait(false);
                                updated = await _notificationStorage.StoreEnvelopeAsync(owner, notification).ConfigureAwait(false);
                            }
                        }
                    }
                    else
                    {
                        await _notificationStorage.StoreEnvelopeAsync(owner, notification).ConfigureAwait(false);
                    }
                }
            }
            else if (envelope is Message)
            {
                var message = (Message)envelope;
                await _messageStorage.StoreEnvelopeAsync(envelope.To.ToIdentity(), message).ConfigureAwait(false);
            }
            else if (envelope is Command)                     
            {
                var command = (Command)envelope;

                TaskCompletionSource<Command> commandTcs;
                if (_pendingCommandsDictionary.TryRemove(envelope.Id, out commandTcs))
                {
                    commandTcs.TrySetResult(command);                                       
                }                
            }
       
        }

        public override Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            return _inputBufferBlock.ReceiveAsync(cancellationToken);
        }

        public override Task OpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            var session = new Session()
            {
                State = SessionState.New
            };

            return SendToInputBufferAsync(session, cancellationToken);
        }

        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            // Close the buffers            
            _inputBufferBlock.Complete();
            _sessionTaskCompletionSource.TrySetCanceled();

            var pendingCommandIds = _pendingCommandsDictionary.Keys.ToArray();
            foreach (var commandId in pendingCommandIds)
            {
                TaskCompletionSource<Command> tcs;

                if (_pendingCommandsDictionary.TryRemove(commandId, out tcs))
                {
                    tcs.TrySetCanceled();
                }
            }

            var pendingNotificationIds = _pendingNotificationsDictionary.Keys.ToArray();
            foreach (var notification in pendingNotificationIds)
            {
                TaskCompletionSource<Notification> tcs;

                if (_pendingNotificationsDictionary.TryRemove(notification, out tcs))
                {
                    tcs.TrySetCanceled();
                }
            }

            return Task.FromResult<object>(null);
        } 

        #endregion

        #region Private Methods

        private async Task ReceiveSessionAsync(CancellationToken cancellationToken, Session session)
        {
            if (session.State == SessionState.Negotiating)
            {
                if (session.CompressionOptions != null &&
                    session.EncryptionOptions != null)
                {
                    var responseSession = new Session()
                    {
                        Id = session.Id,
                        State = SessionState.Negotiating,
                        Compression = this.Compression,
                        Encryption = this.Encryption
                    };

                    if (!await SendToInputBufferAsync(responseSession, cancellationToken).ConfigureAwait(false))
                    {                        
                        throw new InvalidOperationException("The input buffer is complete");                        
                    }
                }
            }
            else if (session.State == SessionState.Authenticating)
            {
                var responseSession = new Session()
                {
                    Id = session.Id
                };

                if (session.SchemeOptions != null &&
                    session.SchemeOptions.Any(s => s == AuthenticationScheme.Plain))
                {
                    var identity = Identity.Parse(_httpIdentity.Name);
                    responseSession.State = SessionState.Authenticating;
                    responseSession.From = new Node()
                    {
                        Name = identity.Name,
                        Domain = identity.Domain
                    };

                    var plainAuthentication = new PlainAuthentication();
                    plainAuthentication.SetToBase64Password(_httpIdentity.Password);
                    responseSession.Authentication = plainAuthentication;
                }
                else
                {
                    // Unsupported authentication scheme
                    responseSession.State = SessionState.Finishing;
                }

                if (!await SendToInputBufferAsync(responseSession, cancellationToken).ConfigureAwait(false))
                {                    
                    throw new InvalidOperationException("The input buffer is complete");                    
                }
            }
            else
            {
                // Remove the identity from the memory, since is not necessary anymore
                _httpIdentity = null;

                // Completes the task
                _sessionTaskCompletionSource.SetResult(session);                
            }
        }

        #endregion
    }
}