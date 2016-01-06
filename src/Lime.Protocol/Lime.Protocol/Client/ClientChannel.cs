using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Security;

namespace Lime.Protocol.Client
{
    /// <summary>
    /// Defines the communication channel between a node and a server.
    /// </summary>
    public class ClientChannel : ChannelBase, IClientChannel, IDisposable
    {
        #region Private fields

        private readonly bool _autoNotifyReceipt;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <a href="ClientChannel"/> class.
        /// </summary>
        /// <param name="transport">The transport to be used by the channel.</param>
        /// <param name="sendTimeout">The channel send timeout.</param>
        /// <param name="autoReplyPings">Indicates if the channel should reply automatically to ping request commands. In this case, the ping command are not returned by the ReceiveCommandAsync method.</param>
        /// <param name="autoNotifyReceipt">Indicates if the client should automatically send 'received' notifications for messages.</param>
        /// <param name="remotePingInterval">The interval to ping the remote party.</param>
        /// <param name="remoteIdleTimeout">The timeout to close the channel due to inactivity.</param>
        public ClientChannel(ITransport transport, TimeSpan sendTimeout, int buffersLimit = 5, bool fillEnvelopeRecipients = false, bool autoReplyPings = true, bool autoNotifyReceipt = false, TimeSpan? remotePingInterval = null, TimeSpan? remoteIdleTimeout = null)
            : base(transport, sendTimeout, buffersLimit, fillEnvelopeRecipients, autoReplyPings, remotePingInterval, remoteIdleTimeout)
        {
            _autoNotifyReceipt = autoNotifyReceipt;
        }

        ~ClientChannel()
        {
            Dispose(false);
        }

        #endregion

        #region IClientChannel Members

        /// <summary>
        /// Sends a new session envelope
        /// to the server and awaits for
        /// the response.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Cannot await for a session response since there's already a listener.</exception>
        public async Task<Session> StartNewSessionAsync(CancellationToken cancellationToken)
        {
            if (State != SessionState.New)
            {
                throw new InvalidOperationException(string.Format("Cannot start a session in the '{0}' state.", State));
            }

            var session = new Session
            {
                State = SessionState.New
            };

            await SendSessionAsync(session).ConfigureAwait(false);
            return await ReceiveSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a negotiate session envelope
        /// to accepts the session negotiation options
        /// and awaits for the server confirmation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="sessionCompression">The session compression option</param>
        /// <param name="sessionEncryption">The session encryption option</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">
        /// Cannot await for a session response since there's already a listener.
        /// </exception>
        public async Task<Session> NegotiateSessionAsync(SessionCompression sessionCompression, SessionEncryption sessionEncryption, CancellationToken cancellationToken)
        {
            if (State != SessionState.Negotiating)
            {
                throw new InvalidOperationException(string.Format("Cannot negotiate a session in the '{0}' state", State));
            }

            var session = new Session
            {
                Id = SessionId,
                State = SessionState.Negotiating,
                Compression = sessionCompression,
                Encryption = sessionEncryption
            };

            await SendSessionAsync(session).ConfigureAwait(false);
            return await ReceiveSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Receives a authenticating session envelope
        /// from the server, after a session negotiation.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// An authenticating session envelope or a failed session envelope.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Cannot await for a session response since there's already a listener.
        /// </exception>
        [Obsolete("Use ReceiveSessionAsync instead")]
        public async Task<Session> ReceiveAuthenticatingSessionAsync(CancellationToken cancellationToken)
        {
            if (State != SessionState.Negotiating)
            {
                throw new InvalidOperationException(string.Format("Cannot receive a authenticating session in the '{0}' state", State));
            }

            return await ReceiveSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a authenticate session envelope to the server to establish an authenticated session and awaits for the response.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="identity"></param>
        /// <param name="authentication"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">
        /// Cannot await for a session response since there's already a listener.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// identity
        /// or
        /// authentication
        /// </exception>
        public async Task<Session> AuthenticateSessionAsync(Identity identity, Authentication authentication, string instance, CancellationToken cancellationToken)
        {
            if (State != SessionState.Authenticating)
            {
                throw new InvalidOperationException(string.Format("Cannot authenticate a session in the '{0}' state", State));
            }

            if (identity == null) throw new ArgumentNullException(nameof(identity));
            if (authentication == null) throw new ArgumentNullException(nameof(authentication));

            var session = new Session
            {
                Id = SessionId,
                From = new Node
                {
                    Name = identity.Name,
                    Domain = identity.Domain,
                    Instance = instance
                },
                State = SessionState.Authenticating,
                Authentication = authentication
            };

            await SendSessionAsync(session).ConfigureAwait(false);
            return await ReceiveSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Notify to the server that
        /// the specified message was received
        /// by the peer
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">to</exception>
        public Task SendReceivedNotificationAsync(Guid messageId, Node to)
        {
            if (to == null) throw new ArgumentNullException(nameof(to));

            var notification = new Notification
            {
                Id = messageId,
                To = to,
                Event = Event.Received
            };

            return SendNotificationAsync(notification);
        }

        /// <summary>
        /// Sends a finishing session 
        /// envelope to the server.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task SendFinishingSessionAsync()
        {
            if (State != SessionState.Established)
            {
                throw new InvalidOperationException(string.Format("Cannot finish a session in the '{0}' state", State));
            }

            var session = new Session
            {
                Id = SessionId,
                State = SessionState.Finishing
            };

            return SendSessionAsync(session);
        }

        /// <summary>
        /// Receives a finished session envelope
        /// from the server.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">
        /// Cannot await for a session response since there's already a listener.
        /// </exception>
        public Task<Session> ReceiveFinishedSessionAsync(CancellationToken cancellationToken)
        {
            if (State != SessionState.Established)
            {
                throw new InvalidOperationException(string.Format("Cannot receive a finished session in the '{0}' state", State));
            }

            return ReceiveSessionAsync(cancellationToken);
        }

        #endregion

        #region ChannelBase Members

        /// <summary>
        /// Receives a message
        /// from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public override async Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            var message = await base.ReceiveMessageAsync(cancellationToken).ConfigureAwait(false);

            if (_autoNotifyReceipt &&
                message.Id != Guid.Empty &&
                message.From != null)
            {
                await SendReceivedNotificationAsync(message.Id, message.From).ConfigureAwait(false);
            }

            return message;
        }

        /// <summary>
        /// Receives a session
        /// from the remote node.
        /// Avoid to use this method directly. Instead,
        /// use the Server or Client channel methods.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public override async Task<Session> ReceiveSessionAsync(CancellationToken cancellationToken)
        {
            var session = await base.ReceiveSessionAsync(cancellationToken).ConfigureAwait(false);

            SessionId = session.Id;
            State = session.State;

            if (session.State == SessionState.Established &&
                session.Id != Guid.Empty)
            {
                LocalNode = session.To;
                RemoteNode = session.From;
            }
            else if (session.State == SessionState.Finished || 
                     session.State == SessionState.Failed)
            {
                await Transport.CloseAsync(cancellationToken).ConfigureAwait(false);
            }

            return session;
        }

        /// <summary>
        /// Fills the envelope recipients using the session information.
        /// </summary>
        /// <param name="envelope"></param>
        /// <param name="isSending"></param>
        protected override void FillEnvelope(Envelope envelope, bool isSending)
        {
            base.FillEnvelope(envelope, isSending);

            if (isSending &&
                LocalNode != null)
            {
                if (envelope.Pp == null)
                {
                    if (envelope.From != null &&
                        !envelope.From.Equals(LocalNode))
                    {
                        envelope.Pp = LocalNode.Copy();
                    }
                }
                else if (string.IsNullOrWhiteSpace(envelope.Pp.Domain))
                {
                    envelope.Pp.Domain = LocalNode.Domain;
                }
            }
        }

        /// <summary>
        /// Closes the idle channel with the remote party.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task OnRemoteIdleAsync(CancellationToken cancellationToken)
        {
            await SendFinishingSessionAsync().ConfigureAwait(false);
            await ReceiveFinishedSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}