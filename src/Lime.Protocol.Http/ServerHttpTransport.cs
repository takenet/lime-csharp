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
    internal class ServerHttpTransport : TransportBase, ITransportSession
    {
        #region Private Fields

        private HttpListenerBasicIdentity _httpIdentity;
        private readonly BufferBlock<Envelope> _inputBufferBlock;
        private readonly IEnvelopeStorage<Message> _messageStorage;
        private readonly IEnvelopeStorage<Notification> _notificationStorage;

        private TaskCompletionSource<Session> _authenticationTaskCompletionSource;
        private TaskCompletionSource<Session> _closingTaskCompletionSource;

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

            if (messageStorage == null)
            {
                throw new ArgumentNullException("messageStorage");
            }

            _messageStorage = messageStorage;

            if (notificationStorage == null)
            {
                throw new ArgumentNullException("notificationStorage");
            }
            _notificationStorage = notificationStorage;

            _inputBufferBlock = new BufferBlock<Envelope>();
            _pendingNotificationsDictionary = new ConcurrentDictionary<Guid, TaskCompletionSource<Notification>>();
            _pendingCommandsDictionary = new ConcurrentDictionary<Guid, TaskCompletionSource<Command>>();

        }

        #endregion

        #region IEmulatedTransport Members

        public DateTimeOffset Expiration
        {
            get { return DateTimeOffset.UtcNow; }
        }

        /// <summary>
        /// Adds the envelope to the buffer that
        /// the server reads the envelopes
        /// sent by the node.
        /// </summary>
        public async Task SubmitAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (!await _inputBufferBlock.SendAsync(envelope, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The input buffer is complete");
            }
        }

        /// <summary>
        /// Sends a message to the input buffer
        /// and awaits for a notification.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Invalid message id</exception>
        /// <exception cref="System.InvalidOperationException">
        /// Could not register the message
        /// or
        /// The input buffer is complete
        /// </exception>
        public async Task<Notification> ProcessMessageAsync(Message message, CancellationToken cancellationToken)
        {
            if (message.Id.Equals(Guid.Empty))
            {
                throw new ArgumentException("Invalid message id");
            }

            var tcs = new TaskCompletionSource<Notification>();
            cancellationToken.Register(() => 
                {
                    tcs.TrySetCanceled();
                    _pendingNotificationsDictionary.TryRemove(message.Id, out tcs);
                });
            
            if (!_pendingNotificationsDictionary.TryAdd(message.Id, tcs))
            {
                throw new InvalidOperationException("Could not register the message");
            }

            await SubmitAsync(message, cancellationToken).ConfigureAwait(false);            

            return await tcs.Task;
        }

        /// <summary>
        /// Sends a command to the input buffer
        /// and awaits for the response.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">
        /// Invalid command id;command
        /// or
        /// Invalid command status;command
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Could not register the command
        /// or
        /// The input buffer is complete
        /// </exception>
        public async Task<Command> ProcessCommandAsync(Command command, CancellationToken cancellationToken)
        {
            if (command.Id.Equals(Guid.Empty))
            {
                throw new ArgumentException("Invalid command id", "command");
            }

            if (command.Status != CommandStatus.Pending)
            {
                throw new ArgumentException("Invalid command status", "command");
            }

            var tcs = new TaskCompletionSource<Command>();
            cancellationToken.Register(() => 
                {
                    tcs.TrySetCanceled();
                    _pendingCommandsDictionary.TryRemove(command.Id, out tcs);
                });
            
            if (!_pendingCommandsDictionary.TryAdd(command.Id, tcs))
            {
                throw new InvalidOperationException("Could not register the command");
            }

            await SubmitAsync(command, cancellationToken).ConfigureAwait(false);

            return await tcs.Task;
        }

        /// <summary>
        /// Gets the session state associated to the transport channel.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public Task<Session> AuthenticateAsync(CancellationToken cancellationToken)
        {
            if (_authenticationTaskCompletionSource != null)
            {
                throw new InvalidOperationException("An authentication process is already active.");
            }

            _authenticationTaskCompletionSource = new TaskCompletionSource<Session>();            
            cancellationToken.Register(() => _authenticationTaskCompletionSource.TrySetCanceled());

            return _authenticationTaskCompletionSource.Task;
        }

        public async Task FinishAsync(CancellationToken cancellationToken)
        {
            if (_closingTaskCompletionSource != null)
            {
                throw new InvalidOperationException("The transport closing process is already active.");
            }

            if (_authenticationTaskCompletionSource == null)
            {
                throw new InvalidOperationException("Cannot close an unauthenticated transport");
            }

            _closingTaskCompletionSource = new TaskCompletionSource<Session>();
            cancellationToken.Register(() => _closingTaskCompletionSource.TrySetCanceled());

            var existingSession = await _authenticationTaskCompletionSource.Task.ConfigureAwait(false);
            if (existingSession.State == SessionState.Established)
            {
                var session = new Session()
                {
                    Id = existingSession.Id,
                    State = SessionState.Finishing
                };

                await SubmitAsync(session, cancellationToken).ConfigureAwait(false);
                await _closingTaskCompletionSource.Task.ConfigureAwait(false);
            }                                 
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

        /// <summary>
        /// Sends an envelope to the node. 
        /// If there's no pending HTTP request
        /// for the envelope, it will be stored.
        /// </summary>
        /// <param name="envelope">The envelope.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public override async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope is Session)
            {
                // The session negotiation is emulated by the transport
                var session = (Session)envelope;
                await ProcessSentSessionAsync(session, cancellationToken).ConfigureAwait(false);
            }
            else if (envelope is Notification)
            {
                var notification = (Notification)envelope;
                await ProcessSentNotificationAsync(notification).ConfigureAwait(false);
            }
            else if (envelope is Message)
            {
                var message = (Message)envelope;
                await _messageStorage.StoreEnvelopeAsync(message.To.ToIdentity(), message).ConfigureAwait(false);
            }
            else if (envelope is Command)                     
            {
                var command = (Command)envelope;

                TaskCompletionSource<Command> commandTcs;
                if (_pendingCommandsDictionary.TryRemove(command.Id, out commandTcs))
                {
                    commandTcs.TrySetResult(command);                                       
                }                
            }       
        }

        /// <summary>
        /// Receives an envelope
        /// from the buffer.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public override Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            return _inputBufferBlock.ReceiveAsync(cancellationToken);
        }

        /// <summary>
        /// Opens the asynchronous.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public override Task OpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            var session = new Session()
            {
                State = SessionState.New
            };

            return SubmitAsync(session, cancellationToken);
        }

        /// <summary>
        /// Completes the input buffer
        /// and cancel all pending requests.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            // Close the buffers            
            _inputBufferBlock.Complete();
            _authenticationTaskCompletionSource.TrySetCanceled();

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

        private async Task ProcessSentSessionAsync(Session session, CancellationToken cancellationToken)
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

                    await SubmitAsync(responseSession, cancellationToken).ConfigureAwait(false);
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

                await SubmitAsync(responseSession, cancellationToken).ConfigureAwait(false);
            }
            else if (_authenticationTaskCompletionSource != null &&
                     !_authenticationTaskCompletionSource.Task.IsCompleted)
            {
                // Remove the identity from the memory, since is not necessary anymore
                _httpIdentity = null;

                // Completes the session task
                _authenticationTaskCompletionSource.TrySetResult(session);                
            }
            else if (_closingTaskCompletionSource != null &&
                     !_closingTaskCompletionSource.Task.IsCompleted)
            {
                _closingTaskCompletionSource.TrySetResult(session);
            }
        }

        private async Task ProcessSentNotificationAsync(Notification notification)
        {
            TaskCompletionSource<Notification> notificationTcs;
            if (_pendingNotificationsDictionary.TryRemove(notification.Id, out notificationTcs))
            {
                notificationTcs.TrySetResult(notification);
            }
            else
            {
                var owner = notification.To.ToIdentity();
                var existingNotification = await _notificationStorage.GetEnvelopeAsync(owner, notification.Id).ConfigureAwait(false);
                if (existingNotification != null)
                {
                    if (notification.Event == Event.Failed ||
                        existingNotification.Event < notification.Event)
                    {
                        bool updated = false;
                        int tryCount = 0;

                        while (!updated && tryCount++ < 3)
                        {
                            await _notificationStorage.DeleteEnvelopeAsync(owner, notification.Id).ConfigureAwait(false);
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

        #endregion

    }
}