using Lime.Protocol.Network;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Http
{
    public interface IHttpTransportProvider
    {
        /// <summary>
        /// Gets the transport
        /// for the specified principal.
        /// </summary>
        /// <param name="requestPrincipal">The request principal.</param>
        /// <param name="cacheInstance">if set to <c>true</c> the transport instance should be cached.</param>
        /// <returns></returns>
        ITransportSession GetTransport(IPrincipal requestPrincipal, bool cacheInstance);

        /// <summary>
        /// Occurs when a new transport is created.
        /// </summary>
        event EventHandler<TransportEventArgs> TransportCreated;
    }

    public class TransportEventArgs : EventArgs
    {
        public TransportEventArgs(ITransport transport)
        {
            if (transport == null)
            {
                throw new ArgumentNullException("transport");
            }

            Transport = transport;
        }

        public ITransport Transport { get; private set; }
    }
}
