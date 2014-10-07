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
        /// <returns></returns>
        ServerHttpTransport GetTransport(IPrincipal requestPrincipal);

        /// <summary>
        /// Occurs when a new transport is created.
        /// </summary>
        event EventHandler<ServerHttpTransportEventArgs> TransportCreated;
    }

    public class ServerHttpTransportEventArgs : EventArgs
    {
        public ServerHttpTransportEventArgs(ServerHttpTransport transport)
        {
            if (transport == null)
            {
                throw new ArgumentNullException("transport");
            }

            Transport = transport;
        }

        public ServerHttpTransport Transport { get; private set; }
    }
}
