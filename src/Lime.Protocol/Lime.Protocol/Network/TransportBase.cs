using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Base class for transport
    /// implementation
    /// </summary>
    public abstract class TransportBase : ITransport
    {
        #region Private fields

        private bool _closingInvoked;
        private bool _closedInvoked;

        #endregion

        #region ITransport Members

        /// <summary>
        /// Sends an envelope to 
        /// the connected node
        /// </summary>
        /// <param name="envelope">Envelope to be transported</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task SendAsync(Envelope envelope, CancellationToken cancellationToken);

        /// <summary>
        /// Receives an envelope 
        /// from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public abstract Task<Envelope> ReceiveAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Opens the transport connection with
        /// the specified Uri
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public abstract Task OpenAsync(Uri uri, CancellationToken cancellationToken);

        /// <summary>
        /// Closes the connection
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async virtual Task CloseAsync(CancellationToken cancellationToken)
        {
            await this.OnClosingAsync().ConfigureAwait(false);
            await this.PerformCloseAsync(cancellationToken).ConfigureAwait(false);
            this.OnClosed();
        }

        /// <summary>
        /// Enumerates the supported compression
        /// options for the transport
        /// </summary>
        /// <returns></returns>
        public virtual SessionCompression[] GetSupportedCompression()
        {
            return new SessionCompression[] { SessionCompression.None };
        }

        /// <summary>
        /// Gets the current transport 
        /// compression option
        /// </summary>
        public virtual SessionCompression Compression { get; protected set; }

        /// <summary>
        /// Defines the compression mode
        /// for the transport
        /// </summary>
        /// <param name="compression">The compression mode</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public virtual Task SetCompressionAsync(SessionCompression compression, CancellationToken cancellationToken)
        {
            if (compression != SessionCompression.None)
            {
                throw new NotSupportedException();
            }

            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Enumerates the supported encryption
        /// options for the transport
        /// </summary>
        /// <returns></returns>
        public virtual SessionEncryption[] GetSupportedEncryption()
        {
            return new SessionEncryption[] { SessionEncryption.None };
        }

        /// <summary>
        /// Gets the current transport 
        /// encryption option
        /// </summary>
        public virtual SessionEncryption Encryption { get; protected set; }

        /// <summary>
        /// Defines the encryption mode
        /// for the transport
        /// </summary>
        /// <param name="encryption"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public virtual Task SetEncryptionAsync(SessionEncryption encryption, CancellationToken cancellationToken)
        {
            if (encryption != SessionEncryption.None)
            {
                throw new NotSupportedException();
            }

            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Occurs when the channel is about
        /// to be closed
        /// </summary>
        public event EventHandler<DeferralEventArgs> Closing;

        /// <summary>
        /// Occurs after the connection was closed
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Closes the transport
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task PerformCloseAsync(CancellationToken cancellationToken);
        
        #endregion


        /// <summary>
        /// Raises the Closing event with
        /// a deferral to wait the event handlers
        /// to complete the execution.
        /// </summary>
        /// <returns></returns>
        protected virtual Task OnClosingAsync()
        {
            if (!_closingInvoked)
            {
                _closingInvoked = true;

                var e = new DeferralEventArgs();
                this.Closing.RaiseEvent(this, e);
                return e.WaitForDeferralsAsync();
            }

            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Raises the Closed event.
        /// </summary>
        protected virtual void OnClosed()
        {
            if (!_closedInvoked)
            {
                _closedInvoked = true;
                this.Closed.RaiseEvent(this, EventArgs.Empty);
            }
        }
    }
}
