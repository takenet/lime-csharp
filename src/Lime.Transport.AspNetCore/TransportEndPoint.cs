using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Lime.Transport.AspNetCore
{
    /// <summary>
    /// Provide configurations about an endpoint.
    /// </summary>
    public class TransportEndPoint
    {
        /// <summary>
        /// The transport protocol to be used for communication.
        /// </summary>
        public TransportType Transport { get; set; } = TransportType.Tcp;
        
        /// <summary>
        /// The endpoint for listening for new connections.
        /// </summary>
        public IPEndPoint EndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 55321);
        
        /// <summary>
        /// Defines the server certificate, for TLS connections. (HTTPS, WSS and TLS over TCP).
        /// </summary>
        public X509Certificate2? ServerCertificate { get; set; }
    }
}