using System.Collections.Generic;
using System.Net;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Provides information about a transport connection.
    /// </summary>
    public interface ITransportInformation
    {
        /// <summary>
        /// Gets the current transport compression option.
        /// </summary>
        SessionCompression Compression { get; }

        /// <summary>
        /// Gets the current transport encryption option.
        /// </summary>
        SessionEncryption Encryption { get; }

        /// <summary>
        /// Indicates if the transport is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the local endpoint address.
        /// </summary>
        string LocalEndPoint { get; }

        /// <summary>
        /// Gets the remote endpoint address.
        /// </summary>
        string RemoteEndPoint { get; }

        /// <summary>
        /// Gets specific transport options informations.
        /// </summary>        
        IReadOnlyDictionary<string, object> Options { get; }
    }
}
