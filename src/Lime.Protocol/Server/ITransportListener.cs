using Lime.Protocol.Network;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Server
{
    /// <summary>
    /// Defines a listener interface for the transports.
    /// </summary>
    public interface ITransportListener : IStartable, IStoppable
    {
        /// <summary>
        /// Gets the transport listener URIs.
        /// </summary>
        Uri[] ListenerUris { get; }

        /// <summary>
        /// Accepts a new transport connection.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ITransport> AcceptTransportAsync(CancellationToken cancellationToken);
    }
}
