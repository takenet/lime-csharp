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
        /// Changes the session state and 
        /// sends a negotiate session envelope
        /// to the node with the available 
        /// options
        /// parameters
        /// </summary>
        /// <param name="compressionOptions">The session compression options.</param>
        /// <param name="encryptionOptions"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// sessionCompressionOptions
        /// or
        /// sessionEncriptionOptions
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// No available options for compression negotiation
        /// or
        /// No available options for encryption negotiation
        /// </exception>
        public Task SendNegotiatingSessionAsync(SessionCompression[] compressionOptions, SessionEncryption[] encryptionOptions)
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

            base.State = SessionState.Negotiating;

            var session = new Session()
            {
                Id = base.SessionId,
                From = base.LocalNode,
                To = base.RemoteNode,
                State = base.State,
                CompressionOptions = compressionOptions,
                EncryptionOptions = encryptionOptions
            };

            return base.SendSessionAsync(session);
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
            // It's a problem if both parameters are empty
            if (schemeOptions == null || schemeOptions.Length == 0)
            {
                throw new ArgumentNullException("authentication");
            }

            base.State = SessionState.Authenticating;

            var session = new Session()
            {
                Id = base.SessionId,
                From = base.LocalNode,
                To = base.RemoteNode,
                State = base.State,
                SchemeOptions = schemeOptions
            };

            return base.SendSessionAsync(session);
        }

        /// <summary>
        /// Sends authentication roundtrip information
        /// to the connected node
        /// </summary>
        /// <param name="authenticationRoundtrip">The authentication roundtrip data.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">authenticationRoundtrip</exception>
        /// <exception cref="System.InvalidOperationException">Invalid state for authentication roundtrip</exception>
        public Task SendAuthenticatingSessionAsync(Authentication authenticationRoundtrip)
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
                To = base.RemoteNode,
                State = base.State,
                Authentication = authenticationRoundtrip
            };

            return base.SendSessionAsync(session);
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
                State = base.State
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
        /// <exception cref="System.InvalidOperationException"></exception>
        public async Task SendFailedSessionAsync(Reason reason)
        {
            if (reason == null)
            {
                throw new ArgumentNullException("reason");
            }

            if (base.State != SessionState.Established)
            {
                throw new InvalidOperationException(string.Format("Cannot fail a session in the '{0}' state", this.State));
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
    }
}
