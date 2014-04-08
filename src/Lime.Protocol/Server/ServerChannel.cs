using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol.Security;
using Lime.Protocol.Network;
using System.Threading;

namespace Lime.Protocol.Server
{
    /// <summary>
    /// Defines the communication channel
    /// between a server and a node
    /// </summary>
    public class ServerChannel : ChannelBase, IServerChannel
    {
        #region Constructor

        public ServerChannel(Guid sessionId, Node serverNode, ITransport transport, TimeSpan sendTimeout)
            : base(transport, sendTimeout)
        {
            base.LocalNode = serverNode;
            base.SessionId = sessionId;
        }

        #endregion

        #region IServerChannel Members

        /// <summary>
        /// Receives a new session envelope
        /// from the client node.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">
        /// Cannot await for a session response since there's already a listener.
        /// </exception>
        public Task<Session> ReceiveNewSession(CancellationToken cancellationToken)
        {
            if (base.State != SessionState.New)
            {
                throw new InvalidOperationException(string.Format("Cannot receive a session in the '{0}' state", base.State));                
            }

            if (base._sessionAsyncBuffer.HasPromises)
            {
                throw new InvalidOperationException("Cannot await for a session response since there's already a listener.");
            }

            return base.ReceiveSessionAsync(cancellationToken);
        }

        /// <summary>
        /// Changes the session state and 
        /// sends a negotiate session envelope
        /// to the node with the available 
        /// options and awaits for the client
        /// selected option.
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
        public async Task<Session> NegotiateSessionAsync(CancellationToken cancellationToken, SessionCompression[] compressionOptions, SessionEncryption[] encryptionOptions)
        {
            if (compressionOptions == null)
            {
                throw new ArgumentNullException("compressionOptions");
            }

            if (compressionOptions.Length == 0)
            {
                throw new ArgumentException("No available options for compression negotiation");
            }

            if (encryptionOptions == null)
            {
                throw new ArgumentNullException("encryptionOptions");
            }

            if (encryptionOptions.Length == 0)
            {
                throw new ArgumentException("No available options for encryption negotiation");
            }

            if (base._sessionAsyncBuffer.HasPromises)
            {
                throw new InvalidOperationException("Cannot await for a session response since there's already a listener.");
            }

            base.State = SessionState.Negotiating;

            var session = new Session()
            {
                Id = base.SessionId,
                From = base.LocalNode,
                State = base.State,
                CompressionOptions = compressionOptions,
                EncryptionOptions = encryptionOptions
            };

            await base.SendSessionAsync(session).ConfigureAwait(false);
            return await base.ReceiveSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a negotiate session envelope
        /// to the remote node to choose or
        /// confirm the session negotiation options
        /// parameters
        /// </summary>
        /// <param name="sessionCompression">The session compression option</param>
        /// <param name="sessionEncryption">The session encryption option</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task SendNegotiatingSessionAsync(SessionCompression sessionCompression, SessionEncryption sessionEncryption)
        {
            if (this.State != SessionState.Negotiating)
            {
                throw new InvalidOperationException(string.Format("Cannot negotiate a session in the '{0}' state", this.State));
            }

            var session = new Session()
            {
                Id = this.SessionId,
                State = SessionState.Negotiating,
                Compression = sessionCompression,
                Encryption = sessionEncryption
            };

            return this.SendSessionAsync(session);
        }

        /// <summary>
        /// Changes the session state and 
        /// sends an authenticat envelope
        /// to the node with the available options
        /// </summary>
        /// <param name="schemeOptions"></param>
        /// <param name="authenticationRoundtrip">The authentication roundtrip data.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">authentication</exception>
        public Task SendAuthenticatingSessionAsync(AuthenticationScheme[] schemeOptions)
        {
            if (schemeOptions == null)
            {
                throw new ArgumentNullException("authentication");
            }

            if (schemeOptions.Length == 0)
            {
                throw new ArgumentException("No available options for authentication");
            }

            base.State = SessionState.Authenticating;

            var session = new Session()
            {
                Id = base.SessionId,
                From = base.LocalNode,
                State = base.State,
                SchemeOptions = schemeOptions
            };

            return base.SendSessionAsync(session);
        }


        public async Task<Session> AuthenticateSessionAsync(CancellationToken cancellationToken, AuthenticationScheme[] schemeOptions)
        {
            if (schemeOptions == null)
            {
                throw new ArgumentNullException("authentication");
            }

            if (schemeOptions.Length == 0)
            {
                throw new ArgumentException("No available options for authentication");
            }

            if (base._sessionAsyncBuffer.HasPromises)
            {
                throw new InvalidOperationException("Cannot await for a session response since there's already a listener.");
            }

            base.State = SessionState.Authenticating;

            var session = new Session()
            {
                Id = base.SessionId,
                From = base.LocalNode,
                State = base.State,
                SchemeOptions = schemeOptions
            };

            await base.SendSessionAsync(session).ConfigureAwait(false);
            return await base.ReceiveSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends authentication roundtrip information
        /// to the connected node and awaits
        /// for the client authentication.
        /// </summary>
        /// <param name="authenticationRoundtrip">The authentication roundtrip data.</param>
        /// <returns>
        /// A autheticating session envelope with the authentication information.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">authenticationRoundtrip</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public async Task<Session> AuthenticateSessionAsync(CancellationToken cancellationToken, Authentication authenticationRoundtrip)
        {
            if (authenticationRoundtrip == null)
            {
                throw new ArgumentNullException("authenticationRoundtrip");
            }

            if (base.State != SessionState.Authenticating)
            {
                throw new InvalidOperationException(string.Format("Cannot send an authentication roundtrip for a session in the '{0}' state", this.State));
            }

            var session = new Session()
            {
                Id = base.SessionId,
                From = base.LocalNode,
                State = base.State,
                Authentication = authenticationRoundtrip
            };

            await base.SendSessionAsync(session).ConfigureAwait(false);
            return await base.ReceiveSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Changes the session state and 
        /// sends a finished session envelope
        /// to the node to comunicate the
        /// end of the session
        /// </summary>
        /// <param name="node"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">node</exception>
        public Task SendEstablishedSessionAsync(Node node, SessionMode mode)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (base.State != SessionState.Authenticating)
            {
                throw new InvalidOperationException(string.Format("Cannot establish a session in the '{0}' state", this.State));
            }

            base.RemoteNode = node;
            base.State = SessionState.Established;
            base.Mode = mode;

            var session = new Session()
            {
                Id = base.SessionId,
                From = base.LocalNode,
                To = base.RemoteNode,
                State = base.State,
                Mode = base.Mode
            };

            return base.SendSessionAsync(session);
        }

        /// <summary>
        /// Changes the session state and 
        /// sends a finished session envelope
        /// to the node to comunicate the
        /// end of the session and closes
        /// the transport
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public async Task SendFinishedSessionAsync()
        {
            if (base.State != SessionState.Established)
            {
                throw new InvalidOperationException(string.Format("Cannot finish a session in the '{0}' state", this.State));
            }

            base.State = SessionState.Finished;

            var session = new Session()
            {
                Id = base.SessionId,
                From = base.LocalNode,
                To = base.RemoteNode,
                State = base.State,
                Mode = base.Mode
            };

            await base.SendSessionAsync(session).ConfigureAwait(false);
            await base.Transport.CloseAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Changes the session state and 
        /// sends a failed session envelope
        /// to the node to comunicate the
        /// finished session and closes
        /// the transport
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">reason</exception>
        public async Task SendFailedSessionAsync(Reason reason)
        {
            if (reason == null)
            {
                throw new ArgumentNullException("reason");
            }

            base.State = SessionState.Failed;

            var session = new Session()
            {
                Id = base.SessionId,
                From = base.LocalNode,
                To = base.RemoteNode,
                State = base.State,
                Reason = reason
            };

            await base.SendSessionAsync(session).ConfigureAwait(false);
            await base.Transport.CloseAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Occours when a new session 
        /// request is received by the server
        /// </summary>
        public event EventHandler<EnvelopeEventArgs<Session>> NewSessionReceived;

        /// <summary>
        /// Occours when a finish session request
        /// is received by the server
        /// </summary>
        public event EventHandler<EnvelopeEventArgs<Session>> FinishingSessionReceived;

        #endregion

        #region Event Handlers

        /// <summary>
        /// Raises the SessionReceived event
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected async override Task OnSessionReceivedAsync(Session session)
        {
            await base.OnSessionReceivedAsync(session).ConfigureAwait(false);

            switch (session.State)
            {
                case SessionState.New:
                    this.NewSessionReceived.RaiseEvent(this, new EnvelopeEventArgs<Session>(session));
                    break;
                case SessionState.Finishing:
                    this.FinishingSessionReceived.RaiseEvent(this, new EnvelopeEventArgs<Session>(session));
                    break;
            }
        }

        /// <summary>
        /// Raises the MessageReceived event
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected async override Task OnMessageReceivedAsync(Message message)
        {
            if (this.State == SessionState.Established)
            {
                await base.OnMessageReceivedAsync(message).ConfigureAwait(false);
            }
            else
            {
                var reason = new Reason()
                {
                    Code = ReasonCodes.SESSION_INVALID_ACTION_FOR_STATE,
                    Description = "Invalid action for current session state"
                };

                await this.SendFailedSessionAsync(reason);
            }
        }

        /// <summary>
        /// Raises the CommandReceived event
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        protected async override Task OnCommandReceivedAsync(Command command)
        {
            if (this.State == SessionState.Established)
            {
                await base.OnCommandReceivedAsync(command).ConfigureAwait(false);
            }
            else
            {
                var reason = new Reason()
                {
                    Code = ReasonCodes.SESSION_INVALID_ACTION_FOR_STATE,
                    Description = "Invalid action for current session state"
                };

                await this.SendFailedSessionAsync(reason);
            }
        }

        /// <summary>
        /// Raises the NotificationReceived event
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        protected async override Task OnNotificationReceivedAsync(Notification notification)
        {
            if (this.State == SessionState.Established)
            {
                await base.OnNotificationReceivedAsync(notification).ConfigureAwait(false);
            }
            else
            {
                var reason = new Reason()
                {
                    Code = ReasonCodes.SESSION_INVALID_ACTION_FOR_STATE,
                    Description = "Invalid action for current session state"
                };

                await this.SendFailedSessionAsync(reason);
            }
        }

        #endregion

    }
}
