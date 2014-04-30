using Lime.Protocol.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Server
{
    /// <summary>
    /// Defines a listener interface
    /// for the transports
    /// </summary>
    public interface ITransportListener
    {
        /// <summary>
        /// Gets the transport 
        /// listener URIs.
        /// </summary>
        Uri[] ListenerUris { get; }

        /// <summary>
        /// Start listening for connections.
        /// </summary>
        /// <returns></returns>
        Task StartAsync();

        /// <summary>
        /// Accepts a new transport connection
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stops the tranport listener
        /// </summary>
        /// <returns></returns>
        Task StopAsync();
    }
}
