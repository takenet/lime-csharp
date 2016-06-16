using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Defines a network connection with a node.
    /// </summary>
    public interface ITransport : ITransportInformation
    {
        /// <summary>
        /// Sends an envelope to the remote node.
        /// </summary>
        /// <param name="envelope">Envelope to be transported</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SendAsync(Envelope envelope, CancellationToken cancellationToken);

        /// <summary>
        /// Receives an envelope from the remote node.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Envelope> ReceiveAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Opens the transport connection with the specified Uri.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task OpenAsync(Uri uri, CancellationToken cancellationToken);

        /// <summary>
        /// Closes the connection
        /// <param name="cancellationToken">The cancellation token.</param>
        /// </summary>
        Task CloseAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Enumerates the supported compression options for the transport.
        /// </summary>
        /// <returns></returns>
        SessionCompression[] GetSupportedCompression();

        /// <summary>
        /// Defines the compression mode for the transport.
        /// </summary>
        /// <param name="compression">The compression mode</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SetCompressionAsync(SessionCompression compression, CancellationToken cancellationToken);

        /// <summary>
        /// Enumerates the supported encryption options for the transport.
        /// </summary>
        SessionEncryption[] GetSupportedEncryption();

        /// <summary>
        /// Defines the encryption mode for the transport.
        /// </summary>
        /// <param name="encryption">The encryption mode</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SetEncryptionAsync(SessionEncryption encryption, CancellationToken cancellationToken);

        /// <summary>
        /// Sets a transport option value.
        /// </summary>
        /// <param name="name">Name of the option.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        Task SetOptionAsync(string name, object value);

        /// <summary>
        /// Occurs when the transport is about to be closed.
        /// </summary>
        event EventHandler<DeferralEventArgs> Closing;

        /// <summary>
        /// Occurs after the transport was closed.
        /// </summary>
        event EventHandler Closed;
    }
}
