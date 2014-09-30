using Lime.Protocol.Network;
using Lime.Protocol.Security;
using Lime.Protocol.Util;
using System;
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
        private readonly BufferBlock<Envelope> _outputBufferBlock;
        private readonly TaskCompletionSource<Session> _sessionTaskCompletionSource;

        #endregion

        #region Constructor

        internal ServerHttpTransport(HttpListenerBasicIdentity identity, bool isHttps)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }

            _httpIdentity = identity;
            Compression = SessionCompression.None;
            Encryption = isHttps ? SessionEncryption.TLS : SessionEncryption.None;

            _inputBufferBlock = new BufferBlock<Envelope>();
            _outputBufferBlock = new BufferBlock<Envelope>();

            _sessionTaskCompletionSource = new TaskCompletionSource<Session>();
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Represents the buffer that
        /// the server reads the envelopes
        /// sent by the node.
        /// </summary>
        internal BufferBlock<Envelope> InputBuffer
        {
            get { return _inputBufferBlock; }
        }

        /// <summary>
        /// Represents the buffer that
        /// the server writes the envelopes
        /// to be received by the node.
        /// </summary>
        internal BufferBlock<Envelope> OutputBuffer
        {
            get { return _outputBufferBlock; }
        }

        internal Task<Session> GetSessionAsync(CancellationToken cancellationToken)
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
            else
            {
                if (!await OutputBuffer.SendAsync(envelope, cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException("The output buffer is complete");
                }
            }
        }

        public override Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            return InputBuffer.ReceiveAsync(cancellationToken);
        }

        public override Task OpenAsync(Uri uri, CancellationToken cancellationToken)
        {
            var session = new Session()
            {
                State = SessionState.New
            };

            return InputBuffer.SendAsync(session, cancellationToken);
        }

        protected override Task PerformCloseAsync(CancellationToken cancellationToken)
        {
            // Close the buffers
            InputBuffer.Complete();
            OutputBuffer.Complete();

            _sessionTaskCompletionSource.TrySetCanceled();
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

                    if (!await InputBuffer.SendAsync(responseSession, cancellationToken).ConfigureAwait(false))
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

                if (!await InputBuffer.SendAsync(responseSession, cancellationToken).ConfigureAwait(false))
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