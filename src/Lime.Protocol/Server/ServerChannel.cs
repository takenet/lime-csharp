using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Security;

namespace Lime.Protocol.Server
{
    /// <summary>
    /// Defines the communication channel between a server and a node.
    /// </summary>
    public class ServerChannel : ChannelBase, IServerChannel
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <a href="ServerChannel"/> class.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="serverNode"></param>
        /// <param name="transport"></param>
        /// <param name="sendTimeout"></param>
        /// <param name="buffersLimit"></param>
        /// <param name="fillEnvelopeRecipients"></param>
        /// <param name="autoReplyPings">Indicates if the channel should reply automatically to ping request commands. In this case, the ping command are not returned by the ReceiveCommandAsync method.</param>
        /// <param name="remotePingInterval">The interval to ping the remote party.</param>
        /// <param name="remoteIdleTimeout">The timeout to close the channel due to inactivity.</param>
        public ServerChannel(Guid sessionId, Node serverNode, ITransport transport, TimeSpan sendTimeout, int buffersLimit = 5, bool fillEnvelopeRecipients = false, bool autoReplyPings = false, TimeSpan? remotePingInterval = null, TimeSpan? remoteIdleTimeout = null)
            : base(transport, sendTimeout, buffersLimit, fillEnvelopeRecipients, autoReplyPings, remotePingInterval, remoteIdleTimeout)
        {
            LocalNode = serverNode;
            SessionId = sessionId;
        }

        #endregion

        #region IServerChannel Members

        /// <summary>
        /// Receives a new session envelope from the client node.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">
        /// Cannot await for a session response since there's already a listener.
        /// </exception>
        public async Task<Session> ReceiveNewSessionAsync(CancellationToken cancellationToken)
        {
            if (State != SessionState.New)
            {
                throw new InvalidOperationException($"Cannot receive a new session in the '{State}' state");                
            }

            return await ReceiveSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Changes the session state and sends a negotiate session envelope to the node with the available options and awaits for the client selected option.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="compressionOptions">The session compression options.</param>
        /// <param name="encryptionOptions"></param>
        /// <returns>
        /// A negotiating session envelope with the client node selected options.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// compressionOptions
        /// or
        /// encryptionOptions
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// No available options for compression negotiation
        /// or
        /// No available options for encryption negotiation
        /// </exception>
        /// <exception cref="System.InvalidOperationException">Cannot await for a session response since there's already a listener.</exception>
        public async Task<Session> NegotiateSessionAsync(SessionCompression[] compressionOptions, SessionEncryption[] encryptionOptions, CancellationToken cancellationToken)
        {
            if (State != SessionState.New)
            {
                throw new InvalidOperationException($"Cannot start a session negotiating in the '{State}' state");
            }

            if (compressionOptions == null) throw new ArgumentNullException(nameof(compressionOptions));            
            if (compressionOptions.Length == 0)
            {
                throw new ArgumentException("No available options for compression negotiation");
            }

            if (encryptionOptions == null) throw new ArgumentNullException(nameof(encryptionOptions));            
            if (encryptionOptions.Length == 0)
            {
                throw new ArgumentException("No available options for encryption negotiation");
            }

            State = SessionState.Negotiating;

            var session = new Session
            {
                Id = SessionId,
                From = LocalNode,
                State = State,
                CompressionOptions = compressionOptions,
                EncryptionOptions = encryptionOptions
            };

            await SendSessionAsync(session, cancellationToken).ConfigureAwait(false);
            return await ReceiveSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a negotiate session envelope to the remote node to confirm the session negotiation options.
        /// </summary>
        /// <param name="sessionCompression">The session compression option</param>
        /// <param name="sessionEncryption">The session encryption option</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task SendNegotiatingSessionAsync(SessionCompression sessionCompression, SessionEncryption sessionEncryption, CancellationToken cancellationToken)
        {
            if (State != SessionState.Negotiating)
            {
                throw new InvalidOperationException($"Cannot negotiate a session in the '{State}' state");
            }

            var session = new Session
            {
                Id = SessionId,
                From = LocalNode,
                State = State,
                Compression = sessionCompression,
                Encryption = sessionEncryption
            };

            return SendSessionAsync(session, cancellationToken);
        }

        /// <summary>
        /// Changes the session state and sends an authenticate envelope to the node with the available options and awaits for the client authentication.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="schemeOptions"></param>
        /// <returns>
        /// An authenticating session envelope with the authentication information.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">authentication</exception>
        /// <exception cref="System.ArgumentException">No available options for authentication</exception>
        /// <exception cref="System.InvalidOperationException">Cannot await for a session response since there's already a listener.</exception>
        public async Task<Session> AuthenticateSessionAsync(AuthenticationScheme[] schemeOptions, CancellationToken cancellationToken)
        {
            if (State != SessionState.New &&
                State != SessionState.Negotiating)
            {
                throw new InvalidOperationException($"Cannot start the session authentication in the '{State}' state");
            }

            if (schemeOptions == null) throw new ArgumentNullException(nameof(schemeOptions));
            if (schemeOptions.Length == 0)
            {
                throw new ArgumentException("No available options for authentication", nameof(schemeOptions));
            }

            State = SessionState.Authenticating;

            var session = new Session
            {
                Id = SessionId,
                From = LocalNode,
                State = State,
                SchemeOptions = schemeOptions
            };

            await SendSessionAsync(session, cancellationToken).ConfigureAwait(false);
            return await ReceiveSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends authentication round-trip information to the connected node and awaits for the client authentication.
        /// </summary>
        /// <param name="authenticationRoundtrip">The authentication round-trip data.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// A authenticating session envelope with the authentication information.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">authenticationRoundtrip</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public async Task<Session> AuthenticateSessionAsync(Authentication authenticationRoundtrip, CancellationToken cancellationToken)
        {
            if (authenticationRoundtrip == null) throw new ArgumentNullException(nameof(authenticationRoundtrip));            
            if (State != SessionState.Authenticating)
            {
                throw new InvalidOperationException(
                    $"Cannot send an authentication round-trip for a session in the '{State}' state");
            }

            var session = new Session
            {
                Id = SessionId,
                From = LocalNode,
                State = State,
                Authentication = authenticationRoundtrip
            };

            await SendSessionAsync(session, cancellationToken).ConfigureAwait(false);
            return await ReceiveSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Changes the session state to the established state and sends a session envelope to the node to communicate the establishment of the session.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">node</exception>
        public Task SendEstablishedSessionAsync(Node node, CancellationToken cancellationToken)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));            
            if (State > SessionState.Authenticating)
            {
                throw new InvalidOperationException($"Cannot establish a session in the '{State}' state");
            }

            State = SessionState.Established;
            RemoteNode = node;           

            var session = new Session
            {
                Id = SessionId,
                From = LocalNode,
                To = RemoteNode,
                State = State
            };

            return SendSessionAsync(session, cancellationToken);
        }

        /// <summary>
        /// Receives a finishing session envelope from the client node.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">
        /// Cannot await for a session response since there's already a listener.
        /// </exception>
        public async Task<Session> ReceiveFinishingSessionAsync(CancellationToken cancellationToken)
        {
            if (State != SessionState.Established)
            {
                throw new InvalidOperationException($"Cannot receive a new session in the '{State}' state");
            }

            return await ReceiveSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Changes the session state and sends a finished session envelope to the node to communicate the end of the session and closes the transport.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public async Task SendFinishedSessionAsync(CancellationToken cancellationToken)
        {
            var session = new Session
            {
                Id = SessionId,
                From = LocalNode,
                To = RemoteNode,
                State = SessionState.Finished
            };

            await SendSessionAsync(session, cancellationToken).ConfigureAwait(false);
            await CloseTransportAsync().ConfigureAwait(false);
            State = session.State;
        }

        /// <summary>
        /// Changes the session state and sends a failed session envelope to the node to communicate the finished session and closes the transport.
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">reason</exception>
        public async Task SendFailedSessionAsync(Reason reason, CancellationToken cancellationToken)
        {
            if (reason == null) throw new ArgumentNullException(nameof(reason));
            
            var session = new Session
            {
                Id = SessionId,
                From = LocalNode,
                To = RemoteNode,
                State = SessionState.Failed,
                Reason = reason
            };

            await SendSessionAsync(session, cancellationToken).ConfigureAwait(false);
            await CloseTransportAsync().ConfigureAwait(false);
            State = session.State;
        }

        /// <summary>
        /// Receives a session from the remote node.
        /// Avoid to use this method directly. Instead, use the Server or Client channel methods.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public override async Task<Session> ReceiveSessionAsync(CancellationToken cancellationToken)
        {
            var session = await base.ReceiveSessionAsync(cancellationToken).ConfigureAwait(false);

            if (session.State != SessionState.New && 
                session.Id != SessionId)
            {
                await SendFailedSessionAsync(new Reason
                {
                    Code = ReasonCodes.SESSION_ERROR,
                    Description = "Invalid session id"
                },
                cancellationToken);
            }

            return session;
        }  

        #endregion
    }
}
