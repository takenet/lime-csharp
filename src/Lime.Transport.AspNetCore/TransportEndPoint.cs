using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Lime.Transport.AspNetCore
{
    public class TransportEndPoint
    {
        public TransportType Transport { get; set; } = TransportType.Tcp;
        public IPEndPoint EndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 55321);
        public bool Tls { get; set; } = false;
        public X509Certificate2? ServerCertificate { get; set; }
    }
}