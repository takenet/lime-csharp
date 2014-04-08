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
        /// Start listening connections
        /// at the specified URI
        /// </summary>
        /// <param name="listenerUri"></param>
        /// <returns></returns>
        Task StartAsync(Uri listenerUri);

        /// <summary>
        /// Accepts a new transport connection
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Occurs when a new transport client is
        /// connected to the listener
        /// </summary>
        event EventHandler<TransportEventArgs> Connected;

        /// <summary>
        /// Stops the tranport listener
        /// </summary>
        /// <returns></returns>
        Task StopAsync();
    }
}
