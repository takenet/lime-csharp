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
    internal class ServerHttpTransport : TransportBase
    {

        private HttpListenerBasicIdentity _identity;
        private readonly BufferBlock<Envelope> _inputBufferBlock;
        private readonly BufferBlock<Envelope> _outputBufferBlock;

        private TaskCompletionSource<Session> _sessionTaskCompletionSource;

        #region Constructor

        public ServerHttpTransport(HttpListenerBasicIdentity identity, bool isHttps)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }

            _identity = identity;
            Compression = SessionCompression.None;
            Encryption = isHttps ? SessionEncryption.TLS : SessionEncryption.None;

            _inputBufferBlock = new BufferBlock<Envelope>();
            _outputBufferBlock = new BufferBlock<Envelope>();

            _sessionTaskCompletionSource = new TaskCompletionSource<Session>();
        }

        #endregion

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

                        await InputBuffer.SendAsync(responseSession, cancellationToken).ConfigureAwait(false);
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
                        var identity = Identity.Parse(_identity.Name);
                        responseSession.State = SessionState.Authenticating;
                        responseSession.From = new Node()
                        {
                            Name = identity.Name,
                            Domain = identity.Domain
                        };

                        var plainAuthentication = new PlainAuthentication();
                        plainAuthentication.SetToBase64Password(_identity.Password);
                        responseSession.Authentication = plainAuthentication;
                    }
                    else
                    {
                        // Unsupported authentication scheme
                        responseSession.State = SessionState.Finishing;
                    }

                    await InputBuffer.SendAsync(responseSession, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Remove the identity from the memory, since is not necessary anymore
                    _identity = null;
                    _sessionTaskCompletionSource.SetResult(session);
                }
            }
            else
            {
                await OutputBuffer.SendAsync(envelope, cancellationToken).ConfigureAwait(false);
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
            // TODO: Close the buffers

            _sessionTaskCompletionSource.TrySetCanceled();
            return Task.FromResult<object>(null);
        } 

        #endregion
    }
}