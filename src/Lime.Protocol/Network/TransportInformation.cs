using System.Collections.Generic;
using System.Net;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Provides information about a transport connection.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Network.ITransportInformation" />
    public class TransportInformation : ITransportInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransportInformation"/> class.
        /// </summary>
        /// <param name="compression">The compression.</param>
        /// <param name="encryption">The encryption.</param>
        /// <param name="isConnected">if set to <c>true</c> [is connected].</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="options">The options.</param>
        public TransportInformation(
            SessionCompression compression, 
            SessionEncryption encryption, 
            bool isConnected, 
            string localEndPoint, 
            string remoteEndPoint, 
            IReadOnlyDictionary<string, object> options)
        {
            Compression = compression;
            Encryption = encryption;
            IsConnected = isConnected;
            LocalEndPoint = localEndPoint;
            RemoteEndPoint = remoteEndPoint;
            Options = options;
        }

        /// <summary>
        /// Gets the current transport compression option.
        /// </summary>
        public SessionCompression Compression { get; }

        /// <summary>
        /// Gets the current transport encryption option.
        /// </summary>
        public SessionEncryption Encryption { get; }

        /// <summary>
        /// Indicates if the transport is connected.
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Gets the local endpoint address.
        /// </summary>
        public string LocalEndPoint { get; }

        /// <summary>
        /// Gets the remote endpoint address.
        /// </summary>
        public string RemoteEndPoint { get; }

        /// <summary>
        /// Gets specific transport options informations.
        /// </summary>
        public IReadOnlyDictionary<string, object> Options { get; }
    }
}