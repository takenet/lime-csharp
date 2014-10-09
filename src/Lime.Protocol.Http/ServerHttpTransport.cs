using Lime.Protocol.Http.Storage;
using Lime.Protocol.Network;
using Lime.Protocol.Resources;
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

        private readonly Identity _identity;
        private Authentication _authentication;        
        private readonly IEnvelopeStorage<Message> _messageStorage;
        private readonly IEnvelopeStorage<Notification> _notificationStorage;
        private readonly TimeSpan _expirationInactivityInternal;

        private readonly TaskCompletionSource<Session> _authenticationTaskCompletionSource;
        private readonly TaskCompletionSource<Session> _closingTaskCompletionSource;
        private readonly BufferBlock<Envelope> _inputBufferBlock;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Notification>> _pendingNotificationsDictionary;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Command>> _pendingCommandsDictionary;

        #endregion

        #region Constructor

        internal ServerHttpTransport(Identity identity, Authentication authentication, bool isHttps, IEnvelopeStorage<Message> messageStorage, IEnvelopeStorage<Notification> notificationStorage, TimeSpan expirationInactivityInternal = default(TimeSpan))
        {
            if (identity == null)
            {
                throw new ArgumentNullException("authentication");
            }

            _identity = identity;

            if (authentication == null)
            {
                throw new ArgumentNullException("authentication");
            }

            _authentication = authentication;
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

            if (expirationInactivityInternal.Equals(default(TimeSpan)))
            {
                _expirationInactivityInternal = TimeSpan.FromSeconds(60);
            }
            else
            {
                _expirationInactivityInternal = expirationInactivityInternal;
            }

            Expiration = DateTimeOffset.UtcNow.Add(_expirationInactivityInternal);

            _authenticationTaskCompletionSource = new TaskCompletionSource<Session>();
            _closingTaskCompletionSource = new TaskCompletionSource<Session>();
            _inputBufferBlock = new BufferBlock<Envelope>();
            _pendingNotificationsDictionary = new ConcurrentDictionary<Guid, TaskCompletionSource<Notification>>();
            _pendingCommandsDictionary = new ConcurrentDictionary<Guid, TaskCompletionSource<Command>>();
        }

        #endregion

        #region IEmulatedTransport Members

        public DateTimeOffset Expiration { get; private set; }

        /// <summary>
        /// Adds the envelope to the buffer that
        /// the server reads the envelopes
        /// sent by the node.
        /// </summary>
        /// <param name="envelope">The envelope to be sent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">envelope</exception>
        /// <exception cref="System.InvalidOperationException">The input buffer is complete</exception>
        public async Task SubmitAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException("envelope");
            }

            if (!await _inputBufferBlock.SendAsync(envelope, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The input buffer is complete");
            }

            Expiration = DateTimeOffset.UtcNow.Add(_expirationInactivityInternal);
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
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

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
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

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
        public Task<Session> GetSessionAsync(CancellationToken cancellationToken)
        {
            if (!_authenticationTaskCompletionSource.Task.IsCompleted)
            {
                cancellationToken.Register(() => _authenticationTaskCompletionSource.TrySetCanceled());
            }
            
            return _authenticationTaskCompletionSource.Task;
        }

        public async Task FinishAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _closingTaskCompletionSource.TrySetCanceled());
            
            var currentSession = await GetSessionAsync(cancellationToken).ConfigureAwait(false);
            if (currentSession.State == SessionState.Established)
            {
                var finishingSession = new Session()
                {
                    Id = currentSession.Id,
                    State = SessionState.Finishing
                };

                await SubmitAsync(finishingSession, cancellationToken).ConfigureAwait(false);
                var finishedSession = await _closingTaskCompletionSource.Task.ConfigureAwait(false);

                if (finishedSession.State != SessionState.Finished)
                {
                    if (finishedSession.Reason != null)
                    {
                        throw new LimeException(finishedSession.Reason.Code, finishedSession.Reason.Description);
                    }
                    else
                    {
                        throw new LimeException(ReasonCodes.SESSION_ERROR, "The session has failed");
                    }                    
                }
            }
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
                await SendSessionAsync(session, cancellationToken).ConfigureAwait(false);
            }
            else if (envelope is Notification)
            {
                var notification = (Notification)envelope;
                await SendNotificationAsync(notification).ConfigureAwait(false);
            }
            else if (envelope is Message)
            {
                var message = (Message)envelope;
                await SendMessageAsync(message);
            }
            else if (envelope is Command)                     
            {
                var command = (Command)envelope;
                await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
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
            
            if (!_authenticationTaskCompletionSource.Task.IsCompleted)
            {
                _authenticationTaskCompletionSource.TrySetCanceled();
            }

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

        private async Task SendSessionAsync(Session session, CancellationToken cancellationToken)
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
                    
                    responseSession.State = SessionState.Authenticating;
                    responseSession.From = new Node()
                    {
                        Name = _identity.Name,
                        Domain = _identity.Domain
                    };

                    responseSession.Authentication = _authentication;
                }
                else
                {
                    // Unsupported authentication scheme
                    responseSession.State = SessionState.Finishing;
                }

                await SubmitAsync(responseSession, cancellationToken).ConfigureAwait(false);
            }
            else if (_authenticationTaskCompletionSource.TrySetResult(session))
            {
                // Remove the authentication information from the memory, since is not necessary anymore
                _authentication = null;
            }
            else
            {
                _closingTaskCompletionSource.TrySetResult(session);
            }                     
        }

        private async Task SendNotificationAsync(Notification notification)
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

        private async Task SendMessageAsync(Message message)
        {
            if (!await _messageStorage.StoreEnvelopeAsync(message.To.ToIdentity(), message).ConfigureAwait(false))
            {
                throw new InvalidOperationException("Could not store the message");
            }
        }

        private async Task SendCommandAsync(Command command, CancellationToken cancellationToken)
        {
            TaskCompletionSource<Command> commandTcs;
            if (_pendingCommandsDictionary.TryRemove(command.Id, out commandTcs))
            {
                commandTcs.TrySetResult(command);
            }
            else if (command.IsPingRequest())
            {
                var commandResponse = new Command()
                {
                    Id = command.Id,
                    To = command.From,
                    Uri = new LimeUri(UriTemplates.PING)
                };

                await SubmitAsync(commandResponse, cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

    }
}